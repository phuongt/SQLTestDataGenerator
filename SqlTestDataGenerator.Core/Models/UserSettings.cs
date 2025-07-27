namespace SqlTestDataGenerator.Core.Models;

public class UserSettings
{
    public string DatabaseType { get; set; } = "Oracle";
    public string ConnectionString { get; set; } = string.Empty;
    public string LastQuery { get; set; } = @"
-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date, 
       c.NAME AS company_name, c.code AS company_code, r.NAME AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
       CASE 
           WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
           WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= SYSDATE + 30 THEN 'Sắp hết hạn vai trò'
           ELSE 'Đang làm việc'
       END AS work_status
FROM users u
INNER JOIN companies c ON u.company_id = c.id
INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = 1
INNER JOIN roles r ON ur.role_id = r.id
WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
  AND EXTRACT(YEAR FROM u.date_of_birth) = 1989
  AND c.NAME LIKE '%VNEXT%'
  AND r.code LIKE '%DD%'
  AND (u.is_active = 0 OR ur.expires_at <= SYSDATE + 60)
ORDER BY ur.expires_at ASC, u.created_at DESC";
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
    
    // UI Settings
    public bool AutoSaveSettings { get; set; } = true;
    public bool ShowLogWindow { get; set; } = false;
    public bool EnableSSH { get; set; } = true;
    
    // AI Settings
    public bool EnableAIGeneration { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 120;
} 