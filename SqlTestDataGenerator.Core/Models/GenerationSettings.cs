namespace SqlTestDataGenerator.Core.Models;

/// <summary>
/// Configuration settings for data generation
/// </summary>
public class GenerationSettings
{
    /// <summary>
    /// Number of records to generate
    /// </summary>
    public int RecordCount { get; set; } = 10;

    /// <summary>
    /// Database type (MySQL, PostgreSQL, SQL Server)
    /// </summary>
    public string DatabaseType { get; set; } = "MySQL";

    /// <summary>
    /// Connection string for database
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use AI for smart data generation
    /// </summary>
    public bool UseAI { get; set; } = true;

    /// <summary>
    /// OpenAI API key for AI-powered generation
    /// </summary>
    public string? OpenAiApiKey { get; set; }

    /// <summary>
    /// Locale for generating localized data
    /// </summary>
    public string Locale { get; set; } = "en-US";

    /// <summary>
    /// Seed for random number generation (for reproducible results)
    /// </summary>
    public int? RandomSeed { get; set; }

    /// <summary>
    /// Custom parameters for specific placeholders
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> PlaceholderParameters { get; set; } = new();

    /// <summary>
    /// Whether to respect database constraints
    /// </summary>
    public bool RespectConstraints { get; set; } = true;

    /// <summary>
    /// Whether to generate foreign key references
    /// </summary>
    public bool GenerateForeignKeys { get; set; } = true;

    /// <summary>
    /// Maximum attempts for constraint satisfaction
    /// </summary>
    public int MaxConstraintAttempts { get; set; } = 10;

    /// <summary>
    /// Custom data patterns or rules
    /// </summary>
    public List<DataPattern> DataPatterns { get; set; } = new();

    /// <summary>
    /// Output format preferences
    /// </summary>
    public OutputSettings Output { get; set; } = new();

    /// <summary>
    /// Validation settings
    /// </summary>
    public ValidationSettings Validation { get; set; } = new();

    /// <summary>
    /// Performance settings
    /// </summary>
    public PerformanceSettings Performance { get; set; } = new();
}

/// <summary>
/// Custom data pattern for specific fields or scenarios
/// </summary>
public class DataPattern
{
    /// <summary>
    /// Pattern name/identifier
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Field or column this pattern applies to
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Pattern type (regex, format, enum, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Pattern expression or definition
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Example values for this pattern
    /// </summary>
    public List<string> Examples { get; set; } = new();

    /// <summary>
    /// Weight for pattern selection (if multiple patterns match)
    /// </summary>
    public int Weight { get; set; } = 1;
}

/// <summary>
/// Output formatting settings
/// </summary>
public class OutputSettings
{
    /// <summary>
    /// Whether to format SQL for readability
    /// </summary>
    public bool FormatSql { get; set; } = true;

    /// <summary>
    /// Whether to include comments in generated SQL
    /// </summary>
    public bool IncludeComments { get; set; } = true;

    /// <summary>
    /// Whether to batch INSERT statements
    /// </summary>
    public bool BatchInserts { get; set; } = false;

    /// <summary>
    /// Number of records per INSERT batch
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// File encoding for exported SQL
    /// </summary>
    public string FileEncoding { get; set; } = "UTF-8";

    /// <summary>
    /// Line ending style
    /// </summary>
    public string LineEnding { get; set; } = Environment.NewLine;

    /// <summary>
    /// Whether to include DROP statements before INSERT
    /// </summary>
    public bool IncludeDropStatements { get; set; } = false;

    /// <summary>
    /// Whether to include TRUNCATE statements
    /// </summary>
    public bool IncludeTruncateStatements { get; set; } = false;
}

/// <summary>
/// Validation settings for generated data
/// </summary>
public class ValidationSettings
{
    /// <summary>
    /// Whether to validate generated data before execution
    /// </summary>
    public bool ValidateBeforeExecution { get; set; } = true;

    /// <summary>
    /// Whether to validate SQL syntax
    /// </summary>
    public bool ValidateSqlSyntax { get; set; } = true;

    /// <summary>
    /// Whether to validate against database schema
    /// </summary>
    public bool ValidateSchema { get; set; } = true;

    /// <summary>
    /// Whether to validate data constraints
    /// </summary>
    public bool ValidateConstraints { get; set; } = true;

    /// <summary>
    /// Whether to validate foreign key relationships
    /// </summary>
    public bool ValidateForeignKeys { get; set; } = true;

    /// <summary>
    /// Maximum validation errors before stopping
    /// </summary>
    public int MaxValidationErrors { get; set; } = 10;

    /// <summary>
    /// Whether to continue generation despite validation warnings
    /// </summary>
    public bool ContinueOnWarnings { get; set; } = true;
}

/// <summary>
/// Performance settings for data generation
/// </summary>
public class PerformanceSettings
{
    /// <summary>
    /// Maximum number of parallel operations
    /// </summary>
    public int MaxParallelOperations { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Memory limit for data generation (MB)
    /// </summary>
    public int MemoryLimitMB { get; set; } = 512;

    /// <summary>
    /// Timeout for individual operations (seconds)
    /// </summary>
    public int OperationTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout for database operations (seconds)
    /// </summary>
    public int DatabaseTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Whether to use caching for repeated operations
    /// </summary>
    public bool UseCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time (minutes)
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 30;
}

/// <summary>
/// Execution result for data generation operations
/// </summary>
public class ExecutionResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Number of records generated
    /// </summary>
    public int RecordsGenerated { get; set; } = 0;

    /// <summary>
    /// Number of SQL statements executed
    /// </summary>
    public int StatementsExecuted { get; set; } = 0;

    /// <summary>
    /// Total execution time
    /// </summary>
    public TimeSpan ExecutionTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Generated SQL statements
    /// </summary>
    public List<string> GeneratedSql { get; set; } = new();

    /// <summary>
    /// Result data (if applicable)
    /// </summary>
    public System.Data.DataTable? ResultData { get; set; }

    /// <summary>
    /// Error message (if operation failed)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Detailed error information
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Validation results
    /// </summary>
    public List<ValidationMessage> ValidationMessages { get; set; } = new();

    /// <summary>
    /// Performance metrics
    /// </summary>
    public PerformanceMetrics Performance { get; set; } = new();

    /// <summary>
    /// Output file path (if data was exported)
    /// </summary>
    public string? OutputFilePath { get; set; }

    /// <summary>
    /// Additional metadata about the operation
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Performance metrics for data generation operations
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Memory usage during operation (MB)
    /// </summary>
    public double MemoryUsageMB { get; set; } = 0;

    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsagePercent { get; set; } = 0;

    /// <summary>
    /// Database connection time
    /// </summary>
    public TimeSpan DatabaseConnectionTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Schema analysis time
    /// </summary>
    public TimeSpan SchemaAnalysisTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Data generation time
    /// </summary>
    public TimeSpan DataGenerationTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// SQL execution time
    /// </summary>
    public TimeSpan SqlExecutionTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Validation time
    /// </summary>
    public TimeSpan ValidationTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Records generated per second
    /// </summary>
    public double RecordsPerSecond { get; set; } = 0;

    /// <summary>
    /// Number of retries due to constraint violations
    /// </summary>
    public int ConstraintRetries { get; set; } = 0;

    /// <summary>
    /// Cache hit ratio (if caching is enabled)
    /// </summary>
    public double CacheHitRatio { get; set; } = 0;
}