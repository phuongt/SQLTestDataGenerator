using SqlTestDataGenerator.Core.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Configuration;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Centralized logging manager to unify all logging across UI, Tests, and Engine
/// </summary>
public static class CentralizedLoggingManager
{
    private static LoggingConfiguration? _config;
    private static readonly object _configLock = new();
    
    /// <summary>
    /// Initialize centralized logging with configuration
    /// </summary>
    public static void Initialize(LoggingConfiguration? config = null)
    {
        lock (_configLock)
        {
            // First try to read from app.config if available
            _config = config ?? LoadFromAppConfig() ?? new LoggingConfiguration();
            
            // Ensure logs directory exists
            var logsDir = GetLogsDirectory();
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
                Console.WriteLine($"[CentralizedLoggingManager] Created logs directory: {logsDir}");
            }
            
            // Auto-cleanup old logs
            _ = Task.Run(AutoCleanupOldLogsAsync);
            
            Console.WriteLine($"[CentralizedLoggingManager] Initialized. Logs directory: {logsDir}");
            Console.WriteLine($"[CentralizedLoggingManager] Config source: {(_config.ConfigSource ?? "default")}");
        }
    }
    
    /// <summary>
    /// Load configuration from app.config if available
    /// </summary>
    private static LoggingConfiguration? LoadFromAppConfig()
    {
        try
        {
            // Check if we can read from app.config
            var logLevel = ConfigurationManager.AppSettings["LogLevel"];
            var logToFile = ConfigurationManager.AppSettings["LogToFile"];
            var logFilePath = ConfigurationManager.AppSettings["LogFilePath"];
            
            if (string.IsNullOrEmpty(logLevel) && string.IsNullOrEmpty(logToFile) && string.IsNullOrEmpty(logFilePath))
            {
                // No logging config in app.config
                return null;
            }
            
            var config = new LoggingConfiguration
            {
                LogLevel = logLevel ?? "Information",
                ConfigSource = "app.config"
            };
            
            // Parse LogToFile
            if (bool.TryParse(logToFile, out var enableFileLogging))
            {
                config.EnableFileLogging = enableFileLogging;
            }
            
            // Handle LogFilePath
            if (!string.IsNullOrEmpty(logFilePath))
            {
                // Convert old app.config path format to new centralized format
                if (logFilePath.Contains(".txt"))
                {
                    // Old format: "../logs/test-ai-.txt" -> new format: use centralized system
                    Console.WriteLine($"[CentralizedLoggingManager] Converting old LogFilePath format: {logFilePath}");
                    
                    // Extract directory and component info
                    var directory = Path.GetDirectoryName(logFilePath);
                    var fileName = Path.GetFileNameWithoutExtension(logFilePath);
                    
                    if (!string.IsNullOrEmpty(directory))
                    {
                        // Use the directory from app.config
                        config.LogsDirectory = directory.Replace("../", "").Replace("..\\", "");
                    }
                    
                    // Set component type based on filename pattern
                    if (fileName.Contains("test"))
                    {
                        config.DefaultComponent = LogComponent.Test;
                    }
                    else if (fileName.Contains("ai"))
                    {
                        config.DefaultComponent = LogComponent.AI;
                    }
                }
                else
                {
                    // Assume it's a directory path
                    config.LogsDirectory = logFilePath;
                }
            }
            
            Console.WriteLine($"[CentralizedLoggingManager] Loaded config from app.config: LogLevel={config.LogLevel}, LogsDir={config.LogsDirectory}");
            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CentralizedLoggingManager] Could not read app.config: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Get absolute path to logs directory
    /// </summary>
    public static string GetLogsDirectory()
    {
        _config ??= new LoggingConfiguration();
        
        // For test projects, go up one level to project root
        var currentDir = Directory.GetCurrentDirectory();
        if (currentDir.Contains("bin") || currentDir.Contains("Debug") || currentDir.Contains("Release"))
        {
            // Navigate to project root
            var projectRoot = currentDir;
            while (projectRoot.Contains("bin") || projectRoot.Contains("obj"))
            {
                projectRoot = Directory.GetParent(projectRoot)?.FullName ?? currentDir;
            }
            
            // Navigate to solution root
            var solutionRoot = Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
            return Path.Combine(solutionRoot, _config.LogsDirectory);
        }
        
        return Path.Combine(currentDir, _config.LogsDirectory);
    }
    
    /// <summary>
    /// Get log file path for specific component
    /// </summary>
    public static string GetLogFilePath(LogComponent component, DateTime? timestamp = null)
    {
        _config ??= new LoggingConfiguration();
        var logsDir = GetLogsDirectory();
        return _config.FileNaming.GetLogPath(logsDir, component, timestamp);
    }
    
    /// <summary>
    /// Create LoggingSettings for specific component
    /// </summary>
    public static LoggingSettings CreateLoggingSettings(LogComponent component)
    {
        _config ??= new LoggingConfiguration();
        
        return new LoggingSettings
        {
            FilePath = GetLogFilePath(component),
            LogLevel = _config.LogLevel,
            EnableUILogging = _config.EnableUILogging,
            EnableFileLogging = _config.EnableFileLogging,
            MaxFileSizeMB = _config.MaxFileSizeMB
        };
    }
    
    /// <summary>
    /// Clean up old log files automatically
    /// </summary>
    public static async Task AutoCleanupOldLogsAsync()
    {
        try
        {
            _config ??= new LoggingConfiguration();
            var logsDir = GetLogsDirectory();
            
            if (!Directory.Exists(logsDir)) return;
            
            var cutoffDate = DateTime.Now.AddDays(-_config.AutoCleanupAfterDays);
            var logFiles = Directory.GetFiles(logsDir, "*.log");
            var deletedCount = 0;
            
            foreach (var logFile in logFiles)
            {
                var fileInfo = new FileInfo(logFile);
                if (fileInfo.LastWriteTime < cutoffDate)
                {
                    try
                    {
                        File.Delete(logFile);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CentralizedLoggingManager] Failed to delete old log file {logFile}: {ex.Message}");
                    }
                }
            }
            
            if (deletedCount > 0)
            {
                Console.WriteLine($"[CentralizedLoggingManager] Cleaned up {deletedCount} old log files older than {_config.AutoCleanupAfterDays} days");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CentralizedLoggingManager] Auto-cleanup failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get summary of all log files
    /// </summary>
    public static LogsSummary GetLogsSummary()
    {
        var logsDir = GetLogsDirectory();
        var summary = new LogsSummary
        {
            LogsDirectory = logsDir,
            TotalLogFiles = 0,
            TotalSizeMB = 0
        };
        
        if (!Directory.Exists(logsDir))
        {
            return summary;
        }
        
        var logFiles = Directory.GetFiles(logsDir, "*.log");
        summary.TotalLogFiles = logFiles.Length;
        
        foreach (var logFile in logFiles)
        {
            var fileInfo = new FileInfo(logFile);
            summary.TotalSizeMB += (double)fileInfo.Length / (1024 * 1024);
            
            var fileName = fileInfo.Name.ToLower();
            if (fileName.Contains("app")) summary.UILogs++;
            else if (fileName.Contains("engine")) summary.EngineLogs++;
            else if (fileName.Contains("test")) summary.TestLogs++;
            else if (fileName.Contains("ai")) summary.AILogs++;
            else summary.OtherLogs++;
        }
        
        return summary;
    }
}

/// <summary>
/// Summary of logs information
/// </summary>
public class LogsSummary
{
    public string LogsDirectory { get; set; } = string.Empty;
    public int TotalLogFiles { get; set; }
    public double TotalSizeMB { get; set; }
    public int UILogs { get; set; }
    public int EngineLogs { get; set; }
    public int TestLogs { get; set; }
    public int AILogs { get; set; }
    public int OtherLogs { get; set; }
    
    public override string ToString()
    {
        return $"Logs Directory: {LogsDirectory}\n" +
               $"Total Files: {TotalLogFiles}\n" +
               $"Total Size: {TotalSizeMB:F2} MB\n" +
               $"UI: {UILogs}, Engine: {EngineLogs}, Test: {TestLogs}, AI: {AILogs}, Other: {OtherLogs}";
    }
}

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

        // Timer to flush logs to file every 1 second (faster for tests)
        _flushTimer = new Timer(FlushLogsToFile, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
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
        
        // Force immediate flush for test environment
        if (IsTestEnvironment())
        {
            FlushLogsToFile(null);
        }
    }
    
    /// <summary>
    /// Detect if running in test environment
    /// </summary>
    private bool IsTestEnvironment()
    {
        return _settings.FilePath.Contains("test-") || 
               System.Reflection.Assembly.GetExecutingAssembly().FullName?.Contains("Test") == true ||
               Environment.StackTrace.Contains("TestHost") ||
               Environment.StackTrace.Contains("mstest");
    }

    private void FlushLogsToFile(object? state)
    {
        if (_disposed || string.IsNullOrEmpty(_settings.FilePath) || !_settings.EnableFileLogging) return;

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