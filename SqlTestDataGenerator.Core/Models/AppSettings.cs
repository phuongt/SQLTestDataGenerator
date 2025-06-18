using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlTestDataGenerator.Core.Models;

/// <summary>
/// Application settings model for API key and database configuration management
/// </summary>
public class AppSettings
{
    /// <summary>
    /// OpenAI API key for data generation
    /// </summary>
    public string OpenAiApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Default database connection settings
    /// </summary>
    public DatabaseSettings Database { get; set; } = new();

    /// <summary>
    /// Logging configuration
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// Data generation settings
    /// </summary>
    public DataGenerationSettings DataGeneration { get; set; } = new();
}

/// <summary>
/// Database connection settings
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Default database type (SqlServer, MySQL, PostgreSQL)
    /// </summary>
    public string DefaultType { get; set; } = "MySQL";

    /// <summary>
    /// Default connection string
    /// </summary>
    public string DefaultConnectionString { get; set; } = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;";

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120; // Tăng lên 2 phút cho queries phức tạp
}

/// <summary>
/// Logging configuration settings
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Log level (Information, Warning, Error)
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Log file path
    /// </summary>
    public string FilePath { get; set; } = "logs/app.log";

    /// <summary>
    /// Whether to enable real-time UI logging
    /// </summary>
    public bool EnableUILogging { get; set; } = true;

    /// <summary>
    /// Whether to enable file logging
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>
    /// Maximum log file size in MB
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 10;
}

/// <summary>
/// Data generation configuration settings
/// </summary>
public class DataGenerationSettings
{
    /// <summary>
    /// Maximum number of records to generate in one batch
    /// </summary>
    public int MaxRecordsPerBatch { get; set; } = 1000;

    /// <summary>
    /// Default number of records to generate
    /// </summary>
    public int DefaultRecordCount { get; set; } = 10;

    /// <summary>
    /// Whether to use AI for smart data generation
    /// </summary>
    public bool UseAIGeneration { get; set; } = true;

    /// <summary>
    /// AI model to use for generation
    /// </summary>
    public string AIModel { get; set; } = "gpt-3.5-turbo";
}

/// <summary>
/// Centralized logging configuration
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Root logs directory (relative to application root)
    /// </summary>
    public string LogsDirectory { get; set; } = "logs";
    
    /// <summary>
    /// Log file naming pattern
    /// </summary>
    public LogFileNaming FileNaming { get; set; } = new();
    
    /// <summary>
    /// Maximum log file size in MB before rotation
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 10;
    
    /// <summary>
    /// Number of rotated log files to keep
    /// </summary>
    public int MaxRotatedFiles { get; set; } = 5;
    
    /// <summary>
    /// Days after which old log files are automatically deleted
    /// </summary>
    public int AutoCleanupAfterDays { get; set; } = 30;
    
    /// <summary>
    /// Minimum log level to capture
    /// </summary>
    public string LogLevel { get; set; } = "Information";
    
    /// <summary>
    /// Enable file logging
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;
    
    /// <summary>
    /// Enable UI real-time logging
    /// </summary>
    public bool EnableUILogging { get; set; } = true;
    
    /// <summary>
    /// Source where this configuration was loaded from
    /// </summary>
    public string? ConfigSource { get; set; }
    
    /// <summary>
    /// Default component type for logging when not specified
    /// </summary>
    public LogComponent DefaultComponent { get; set; } = LogComponent.Engine;
}

/// <summary>
/// Log file naming configuration
/// </summary>
public class LogFileNaming
{
    /// <summary>
    /// File extension for log files
    /// </summary>
    public string Extension { get; set; } = ".log";
    
    /// <summary>
    /// Date format for log file names
    /// </summary>
    public string DateFormat { get; set; } = "yyyyMMdd";
    
    /// <summary>
    /// Whether to include timestamp in filename
    /// </summary>
    public bool IncludeTimestamp { get; set; } = true;
    
    /// <summary>
    /// Generate log file path for specific component
    /// </summary>
    public string GetLogPath(string logsDirectory, LogComponent component, DateTime? timestamp = null)
    {
        var date = timestamp ?? DateTime.Now;
        var dateStr = IncludeTimestamp ? date.ToString(DateFormat) : "";
        var componentStr = component.ToString().ToLower();
        
        var fileName = IncludeTimestamp 
            ? $"{componentStr}-{dateStr}{Extension}"
            : $"{componentStr}{Extension}";
            
        return Path.Combine(logsDirectory, fileName);
    }
}

/// <summary>
/// Log component types for organized logging
/// </summary>
public enum LogComponent
{
    /// <summary>
    /// UI application logs
    /// </summary>
    App,
    
    /// <summary>
    /// Core engine logs
    /// </summary>
    Engine,
    
    /// <summary>
    /// Test execution logs
    /// </summary>
    Test,
    
    /// <summary>
    /// AI service logs
    /// </summary>
    AI,
    
    /// <summary>
    /// Database operation logs
    /// </summary>
    Database,
    
    /// <summary>
    /// Configuration service logs
    /// </summary>
    Config
} 