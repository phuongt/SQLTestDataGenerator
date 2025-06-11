namespace SqlTestDataGenerator.Core.Models;

public class QueryExecutionRequest
{
    public string DatabaseType { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string SqlQuery { get; set; } = string.Empty;
    public int DesiredRecordCount { get; set; } = 10;
    public int CurrentRecordCount { get; set; } = 0; // Current number of records in database
    public string? OpenAiApiKey { get; set; }
    public bool UseAI { get; set; } = true;
}

public class QueryExecutionResult
{
    public System.Data.DataTable ResultData { get; set; } = new();
    public int GeneratedRecords { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public List<string> GeneratedInserts { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public bool Success { get; set; }
} 