namespace SqlTestDataGenerator.Core.Models;

public class UserSettings
{
    public string DatabaseType { get; set; } = "SQL Server";
    public string ConnectionString { get; set; } = string.Empty;
    public string LastQuery { get; set; } = "SELECT * FROM users";
    public int DefaultRecordCount { get; set; } = 10;
    public string? OpenAiApiKey { get; set; } = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM";
    public bool UseAI { get; set; } = true;
    public bool SaveConnectionHistory { get; set; } = true;
    public List<string> RecentConnections { get; set; } = new();
    public List<string> RecentQueries { get; set; } = new();
    
    /// <summary>
    /// Folder path to export generated SQL INSERT statements
    /// </summary>
    public string SqlExportFolder { get; set; } = "sql-exports";
} 