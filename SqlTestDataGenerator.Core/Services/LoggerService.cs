using SqlTestDataGenerator.Core.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Centralized logging service with structured logging format and UI integration
/// </summary>
public interface ILoggerService : IDisposable
{
    /// <summary>
    /// Log information message
    /// </summary>
    void LogInfo(string message, string? context = null, object? parameters = null);

    /// <summary>
    /// Log warning message
    /// </summary>
    void LogWarning(string message, string? context = null, object? parameters = null);

    /// <summary>
    /// Log error message
    /// </summary>
    void LogError(string message, Exception? exception = null, string? context = null, object? parameters = null);

    /// <summary>
    /// Log method entry
    /// </summary>
    void LogMethodEntry(string methodName, object? parameters = null);

    /// <summary>
    /// Log method exit
    /// </summary>
    void LogMethodExit(string methodName, object? returnValue = null);

    /// <summary>
    /// Event for real-time UI logging
    /// </summary>
    event Action<LogEntry> LogEntryCreated;

    /// <summary>
    /// Get all log entries for UI display
    /// </summary>
    IEnumerable<LogEntry> GetLogEntries();

    /// <summary>
    /// Clear log entries
    /// </summary>
    void ClearLogs();
}

/// <summary>
/// Log entry model for structured logging
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Context { get; set; }
    public string? Parameters { get; set; }
    public string? StackTrace { get; set; }
    public string? Exception { get; set; }
}

/// <summary>
/// Log levels enum
/// </summary>
public enum LogLevel
{
    Information,
    Warning,
    Error
}

/// <summary>
/// Implementation of centralized logging service
/// </summary>
public class LoggerService : ILoggerService
{
    private readonly LoggingSettings _settings;
    private readonly ConcurrentQueue<LogEntry> _logEntries;
    private readonly object _fileLock = new();
    private readonly Timer _flushTimer;
    private readonly Queue<LogEntry> _pendingWrites;
    private bool _disposed = false;

    public event Action<LogEntry>? LogEntryCreated;

    public LoggerService(LoggingSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logEntries = new ConcurrentQueue<LogEntry>();
        _pendingWrites = new Queue<LogEntry>();

        // Ensure log directory exists
        var logDir = Path.GetDirectoryName(_settings.FilePath);
        if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        // Timer to flush logs to file every 5 seconds
        _flushTimer = new Timer(FlushLogsToFile, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public void LogInfo(string message, string? context = null, object? parameters = null)
    {
        if (_disposed) return;

        var entry = CreateLogEntry(LogLevel.Information, message, context, parameters);
        AddLogEntry(entry);
    }

    public void LogWarning(string message, string? context = null, object? parameters = null)
    {
        if (_disposed) return;

        var entry = CreateLogEntry(LogLevel.Warning, message, context, parameters);
        AddLogEntry(entry);
    }

    public void LogError(string message, Exception? exception = null, string? context = null, object? parameters = null)
    {
        if (_disposed) return;

        var entry = CreateLogEntry(LogLevel.Error, message, context, parameters, exception);
        AddLogEntry(entry);
    }

    public void LogMethodEntry(string methodName, object? parameters = null)
    {
        if (_disposed) return;

        var message = $"Method Entry: {methodName}";
        var entry = CreateLogEntry(LogLevel.Information, message, "METHOD_ENTRY", parameters);
        AddLogEntry(entry);
    }

    public void LogMethodExit(string methodName, object? returnValue = null)
    {
        if (_disposed) return;

        var message = $"Method Exit: {methodName}";
        var entry = CreateLogEntry(LogLevel.Information, message, "METHOD_EXIT", returnValue);
        AddLogEntry(entry);
    }

    public IEnumerable<LogEntry> GetLogEntries()
    {
        return _logEntries.ToArray().OrderByDescending(x => x.Timestamp);
    }

    public void ClearLogs()
    {
        while (_logEntries.TryDequeue(out _)) { }
    }

    private LogEntry CreateLogEntry(LogLevel level, string message, string? context = null, object? parameters = null, Exception? exception = null)
    {
        return new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            Context = context,
            Parameters = parameters != null ? System.Text.Json.JsonSerializer.Serialize(parameters) : null,
            Exception = exception?.Message,
            StackTrace = exception?.StackTrace
        };
    }

    private void AddLogEntry(LogEntry entry)
    {
        // Add to memory collection
        _logEntries.Enqueue(entry);

        // Keep only recent entries in memory (last 1000)
        while (_logEntries.Count > 1000)
        {
            _logEntries.TryDequeue(out _);
        }

        // Queue for file writing
        lock (_pendingWrites)
        {
            _pendingWrites.Enqueue(entry);
        }

        // Raise event for UI
        if (_settings.EnableUILogging)
        {
            LogEntryCreated?.Invoke(entry);
        }
    }

    private void FlushLogsToFile(object? state)
    {
        if (_disposed || string.IsNullOrEmpty(_settings.FilePath)) return;

        var entriesToWrite = new List<LogEntry>();

        lock (_pendingWrites)
        {
            while (_pendingWrites.Count > 0)
            {
                entriesToWrite.Add(_pendingWrites.Dequeue());
            }
        }

        if (entriesToWrite.Count == 0) return;

        try
        {
            lock (_fileLock)
            {
                // Check file size and rotate if needed
                if (File.Exists(_settings.FilePath))
                {
                    var fileInfo = new FileInfo(_settings.FilePath);
                    if (fileInfo.Length > _settings.MaxFileSizeMB * 1024 * 1024)
                    {
                        RotateLogFile();
                    }
                }

                // Write entries to file
                using var writer = new StreamWriter(_settings.FilePath, append: true);
                foreach (var entry in entriesToWrite)
                {
                    var logLine = FormatLogEntry(entry);
                    writer.WriteLine(logLine);
                }
            }
        }
        catch (Exception ex)
        {
            // Avoid infinite loop by not logging the logging error
            Debug.WriteLine($"Failed to write to log file: {ex.Message}");
        }
    }

    private void RotateLogFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settings.FilePath);
            var fileName = Path.GetFileNameWithoutExtension(_settings.FilePath);
            var extension = Path.GetExtension(_settings.FilePath);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var rotatedFileName = $"{fileName}_{timestamp}{extension}";
            var rotatedFilePath = Path.Combine(directory ?? "", rotatedFileName);

            File.Move(_settings.FilePath, rotatedFilePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to rotate log file: {ex.Message}");
        }
    }

    private string FormatLogEntry(LogEntry entry)
    {
        var level = entry.Level.ToString().ToUpper().PadRight(11);
        var context = !string.IsNullOrEmpty(entry.Context) ? $" [{entry.Context}]" : "";
        var parameters = !string.IsNullOrEmpty(entry.Parameters) ? $" | Params: {entry.Parameters}" : "";
        var exception = !string.IsNullOrEmpty(entry.Exception) ? $" | Exception: {entry.Exception}" : "";

        return $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {level}{context} | {entry.Message}{parameters}{exception}";
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _flushTimer?.Dispose();

        // Flush any remaining logs
        FlushLogsToFile(null);
    }
} 