using System.Text.Json;

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