using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// STRICT Verification Tests cho Record Count Matching
/// Các tests này REQUIRE record count phải chính xác match với expected
/// </summary>
[TestClass]
[TestCategory("RecordCountVerification")]
public class RecordCountStrictVerificationTests
{
    private const string MYSQL_CONNECTION_STRING = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;";
    private EngineService _engineService = null!;

    [TestInitialize]
    public void Setup()
    {
        _engineService = new EngineService();
    }

    [TestMethod]
    [TestCategory("StrictRecordCount")]
    public async Task ExecuteQueryWithTestDataAsync_SimpleSQL_MustMatchExactRecordCount()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE is_active = 1 ORDER BY created_at DESC LIMIT 10";
        var expectedRecords = 15;
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = MYSQL_CONNECTION_STRING,
            SqlQuery = sql,
            DesiredRecordCount = expectedRecords,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        Console.WriteLine($"=== STRICT TEST: Expected Records = {expectedRecords} ===");
        Console.WriteLine($"SQL: {sql}");

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert - STRICT RECORD COUNT VERIFICATION
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");
        Console.WriteLine($"Expected Records: {expectedRecords}");
        Console.WriteLine($"Match: {result.GeneratedRecords == expectedRecords}");

        if (result.Success)
        {
            // STRICT ASSERTION - MUST MATCH EXACTLY
            Assert.AreEqual(expectedRecords, result.GeneratedRecords, 
                $"Generated records ({result.GeneratedRecords}) MUST match expected ({expectedRecords})");
                
            // Verify data quality
            Assert.IsTrue(result.GeneratedInserts.Count > 0, "Should generate INSERT statements");
            
            Console.WriteLine("✁EPASSED: Record count matches exactly");
        }
        else
        {
            Assert.Fail($"Test FAILED: {result.ErrorMessage}");
        }
    }

    [TestMethod]
    [TestCategory("StrictRecordCount")]
    public async Task ExecuteQueryWithTestDataAsync_MultiTable_MustGenerateCorrectDistribution()
    {
        // Arrange
        var sql = @"
            SELECT u.id, u.username, u.email, 
                   c.name as company_name, 
                   r.name as role_name
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            INNER JOIN user_roles ur ON u.id = ur.user_id
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE u.is_active = 1";
            
        var expectedRecords = 20;
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = MYSQL_CONNECTION_STRING,
            SqlQuery = sql,
            DesiredRecordCount = expectedRecords,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        Console.WriteLine($"=== STRICT MULTI-TABLE TEST: Expected Records = {expectedRecords} ===");
        Console.WriteLine($"Tables: users, companies, user_roles, roles");

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert - STRICT VERIFICATION
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");
        Console.WriteLine($"Generated INSERT Statements: {result.GeneratedInserts.Count}");

        if (result.Success)
        {
            // STRICT ASSERTION
            Assert.AreEqual(expectedRecords, result.GeneratedRecords,
                $"Multi-table generation MUST match expected records exactly");

            // Analyze INSERT distribution
            var insertsByTable = result.GeneratedInserts
                .GroupBy(insert => ExtractTableFromInsert(insert))
                .ToDictionary(g => g.Key, g => g.Count());

            Console.WriteLine("=== INSERT DISTRIBUTION ===");
            foreach (var kvp in insertsByTable)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value} records");
            }

            // Verify all required tables have data
            Assert.IsTrue(insertsByTable.ContainsKey("users"), "Must generate users data");
            Assert.IsTrue(insertsByTable.ContainsKey("companies"), "Must generate companies data");
            Assert.IsTrue(insertsByTable.ContainsKey("roles"), "Must generate roles data");
            Assert.IsTrue(insertsByTable.ContainsKey("user_roles"), "Must generate user_roles data");

            Console.WriteLine("✁EPASSED: Multi-table distribution correct");
        }
        else
        {
            Assert.Fail($"Multi-table test FAILED: {result.ErrorMessage}");
        }
    }

    [TestMethod]
    [TestCategory("StrictRecordCount")]
    public async Task ExecuteQueryWithTestDataAsync_PerformanceWithStrictCount_MustMatch()
    {
        // Arrange
        var sql = @"
            SELECT u.id, u.username, u.first_name, u.last_name, u.email,
                   c.name as company_name, c.code as company_code,
                   r.name as role_name, r.level
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            INNER JOIN user_roles ur ON u.id = ur.user_id
            INNER JOIN roles r ON ur.role_id = r.id
            ORDER BY u.created_at DESC";

        var expectedRecords = 50; // Large count for performance test
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = MYSQL_CONNECTION_STRING,
            SqlQuery = sql,
            DesiredRecordCount = expectedRecords,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        Console.WriteLine($"=== STRICT PERFORMANCE TEST: Expected Records = {expectedRecords} ===");

        // Act
        var startTime = DateTime.Now;
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);
        var endTime = DateTime.Now;
        var duration = endTime - startTime;

        // Assert - STRICT PERFORMANCE + RECORD COUNT
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");
        Console.WriteLine($"Expected Records: {expectedRecords}");
        Console.WriteLine($"Execution Time: {duration.TotalSeconds:F2} seconds");
        Console.WriteLine($"Rate: {result.GeneratedRecords / Math.Max(duration.TotalSeconds, 0.1):F1} records/sec");

        if (result.Success)
        {
            // STRICT PERFORMANCE ASSERTIONS
            Assert.AreEqual(expectedRecords, result.GeneratedRecords,
                $"Performance test MUST generate exactly {expectedRecords} records");
                
            Assert.IsTrue(duration.TotalSeconds < 30, 
                $"Performance test should complete within 30 seconds, took {duration.TotalSeconds:F2}s");
                
            Assert.IsTrue(result.GeneratedInserts.Count > 0, "Should generate INSERT statements");

            var recordsPerSecond = result.GeneratedRecords / Math.Max(duration.TotalSeconds, 0.1);
            Assert.IsTrue(recordsPerSecond >= 1, 
                $"Should generate at least 1 record/second, actual: {recordsPerSecond:F1}");

            Console.WriteLine("✁EPASSED: Performance and record count both correct");
        }
        else
        {
            Assert.Fail($"Performance test FAILED: {result.ErrorMessage}");
        }
    }

    [TestMethod]
    [TestCategory("StrictRecordCount")]
    public async Task ExecuteQueryWithTestDataAsync_EdgeCase_ZeroRecords_MustHandleCorrectly()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE 1=0"; // Always returns 0 rows
        var expectedRecords = 0;
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = MYSQL_CONNECTION_STRING,
            SqlQuery = sql,
            DesiredRecordCount = expectedRecords,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        Console.WriteLine($"=== EDGE CASE TEST: Zero Records ===");

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert - EDGE CASE VERIFICATION
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");

        if (result.Success)
        {
            Assert.AreEqual(0, result.GeneratedRecords, "Should generate exactly 0 records");
            Assert.AreEqual(0, result.GeneratedInserts.Count, "Should generate 0 INSERT statements");
            Console.WriteLine("✁EPASSED: Zero records case handled correctly");
        }
        else
        {
            // For zero records, success/failure depends on implementation
            Console.WriteLine($"Result: {result.ErrorMessage}");
            Assert.IsNotNull(result.ErrorMessage, "Should provide error message for edge case");
        }
    }

    [TestMethod]
    [TestCategory("StrictRecordCount")]
    public async Task ExecuteQueryWithTestDataAsync_VietnameseBusinessQuery_MustMatchExpectedRecords()
    {
        // Arrange - Query chứa Vietnamese business logic từ VOWIS demo
        var sql = @"
            SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, 
                   c.name AS company_name, c.code AS company_code, 
                   r.name AS role_name, r.code AS role_code,
                   CASE 
                       WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                       WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn vai trò'
                       ELSE 'Đang làm việc'
                   END AS work_status
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = 1
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
              AND substr(u.date_of_birth, 1, 4) = '1989'
              AND c.name LIKE '%VNEXT%'
              AND r.code LIKE '%DD%'
            ORDER BY ur.expires_at ASC, u.created_at DESC";
            
        var expectedRecords = 10; // Business-specific count
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = MYSQL_CONNECTION_STRING,
            SqlQuery = sql,
            DesiredRecordCount = expectedRecords,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        Console.WriteLine($"=== VIETNAMESE BUSINESS QUERY TEST: Expected Records = {expectedRecords} ===");
        Console.WriteLine($"Query filters: Phương names, 1989 birth year, VNEXT company, DD role");

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert - BUSINESS LOGIC VERIFICATION
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");

        if (result.Success)
        {
            // STRICT ASSERTION for business query
            Assert.AreEqual(expectedRecords, result.GeneratedRecords,
                $"Vietnamese business query MUST generate exactly {expectedRecords} records");

            // Verify Vietnamese data characteristics
            var userInserts = result.GeneratedInserts.Where(sql => sql.Contains("INSERT INTO users")).ToList();
            Assert.IsTrue(userInserts.Any(insert => insert.Contains("Phương")), 
                "Should generate users with 'Phương' names");
            Assert.IsTrue(userInserts.Any(insert => insert.Contains("1989")), 
                "Should generate users with 1989 birth year");

            var companyInserts = result.GeneratedInserts.Where(sql => sql.Contains("INSERT INTO companies")).ToList();
            Assert.IsTrue(companyInserts.Any(insert => insert.Contains("VNEXT")), 
                "Should generate VNEXT company");

            var roleInserts = result.GeneratedInserts.Where(sql => sql.Contains("INSERT INTO roles")).ToList();
            Assert.IsTrue(roleInserts.Any(insert => insert.Contains("DD")), 
                "Should generate DD roles");

            Console.WriteLine("✁EPASSED: Vietnamese business query data generation correct");
        }
        else
        {
            Assert.Fail($"Vietnamese business query test FAILED: {result.ErrorMessage}");
        }
    }

    private static string ExtractTableFromInsert(string insertStatement)
    {
        if (string.IsNullOrEmpty(insertStatement)) return string.Empty;
        
        var match = System.Text.RegularExpressions.Regex.Match(
            insertStatement, 
            @"INSERT\s+INTO\s+([`\[\w]+)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        return match.Success ? match.Groups[1].Value.Trim('`', '[', ']') : string.Empty;
    }
} 
