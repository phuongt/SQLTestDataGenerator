using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using MySqlConnector;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Integration tests với real MySQL database để reproduce và fix duplicate key issue
/// </summary>
[TestClass]
[TestCategory("MySQLIntegration")]
public class MySQLIntegrationDuplicateKeyTests
{
    private const string REAL_MYSQL_CONNECTION = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;";
    
    private const string VOWIS_BUSINESS_SQL = @"
        SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date, 
               c.NAME AS company_name, c.code AS company_code, r.NAME AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
               CASE 
                   WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                   WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn vai trò'
                   ELSE 'Đang làm việc'
               END AS work_status
        FROM users u
        INNER JOIN companies c ON u.company_id = c.id
        INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = TRUE
        INNER JOIN roles r ON ur.role_id = r.id
        WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
          AND YEAR(u.date_of_birth) = 1989
          AND c.NAME LIKE '%VNEXT%'
          AND r.code LIKE '%DD%'
          AND (u.is_active = 0 OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY))
        ORDER BY ur.expires_at ASC, u.created_at DESC";

    private EngineService _engineService = null!;

    [TestInitialize]
    public void Setup()
    {
        _engineService = new EngineService(DatabaseType.MySQL, REAL_MYSQL_CONNECTION);
    }

    [TestMethod]
    public async Task ExecuteQueryWithTestDataAsync_VowisSQL_ShouldNotProduceDuplicateKeys()
    {
        // CLEANUP: Clear existing data to ensure clean FK references
        Console.WriteLine("🧹 Cleaning up existing data...");
        try
        {
            await CleanupDatabase();
            Console.WriteLine("✅ Database cleanup completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Cleanup warning: {ex.Message}");
            // Continue with test even if cleanup fails
        }

        var request = new QueryExecutionRequest
        {
            SqlQuery = VOWIS_BUSINESS_SQL,
            DatabaseType = "MySQL",
            ConnectionString = REAL_MYSQL_CONNECTION,
            DesiredRecordCount = 10,
            UseAI = true
        };

        Console.WriteLine("🎯 Testing Vowis Business SQL with AI generation...");

        try
        {
            var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

            Assert.IsTrue(result.Success, $"❌ Operation failed: {result.ErrorMessage}");

            Console.WriteLine("✅ SUCCESS: No duplicate key constraint violations!");
            Console.WriteLine($"Generated {result.GeneratedInserts.Count} INSERT statements");
            
            var userRoleStatements = result.GeneratedInserts
                .Where(sql => sql.ToLower().Contains("user_roles"))
                .ToList();

            if (userRoleStatements.Any())
            {
                var combinations = userRoleStatements
                    .Select(sql => ExtractUserRoleCombination(sql))
                    .ToList();

                var uniqueCombinations = combinations.Distinct().ToList();
                var duplicateCount = combinations.Count - uniqueCombinations.Count;

                Console.WriteLine($"📊 user_roles analysis: {combinations.Count} total, {duplicateCount} duplicates");
                
                if (duplicateCount > 0)
                {
                    var duplicates = combinations.GroupBy(x => x)
                        .Where(g => g.Count() > 1)
                        .Select(g => $"{g.Key} (appears {g.Count()} times)")
                        .ToList();
                    
                    Console.WriteLine($"🚨 Duplicate combinations: {string.Join(", ", duplicates)}");
                }

                Assert.AreEqual(0, duplicateCount, 
                    $"❌ Found {duplicateCount} duplicate combinations in user_roles table!");
            }

            Console.WriteLine("✅ Integration test PASSED!");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception occurred: {ex.Message}");
            
            if (ex.Message.Contains("Duplicate entry") && ex.Message.Contains("user_roles.unique_user_role"))
            {
                Console.WriteLine("🎯 REPRODUCED: Exact same duplicate key error từ logs!");
                
                var duplicateMatch = System.Text.RegularExpressions.Regex.Match(
                    ex.Message, @"Duplicate entry '([^']+)'");
                
                if (duplicateMatch.Success)
                {
                    var duplicateCombo = duplicateMatch.Groups[1].Value;
                    Console.WriteLine($"🔍 Duplicate combination: {duplicateCombo}");
                }

                Assert.Fail($"❌ DUPLICATE KEY BUG CONFIRMED: {ex.Message}");
            }
            else
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Cleanup database tables to ensure clean state for testing
    /// </summary>
    private async Task CleanupDatabase()
    {
        using var connection = new MySqlConnector.MySqlConnection(REAL_MYSQL_CONNECTION);
        await connection.OpenAsync();

        // Delete in reverse dependency order to avoid FK constraint violations
        var cleanupQueries = new[]
        {
            "DELETE FROM user_roles WHERE id > 0",
            "DELETE FROM users WHERE id > 0", 
            "DELETE FROM roles WHERE id > 0",
            "DELETE FROM companies WHERE id > 0"
        };

        // Reset auto-increment counters to 1
        var resetQueries = new[]
        {
            "ALTER TABLE companies AUTO_INCREMENT = 1",
            "ALTER TABLE roles AUTO_INCREMENT = 1", 
            "ALTER TABLE users AUTO_INCREMENT = 1",
            "ALTER TABLE user_roles AUTO_INCREMENT = 1"
        };

        foreach (var query in cleanupQueries)
        {
            try
            {
                using var command = new MySqlConnector.MySqlCommand(query, connection);
                var affected = await command.ExecuteNonQueryAsync();
                Console.WriteLine($"  Cleaned {affected} records from {query.Split(' ')[2]}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Cleanup warning for {query}: {ex.Message}");
            }
        }

        foreach (var query in resetQueries)
        {
            try
            {
                using var command = new MySqlConnector.MySqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();
                Console.WriteLine($"  Reset auto-increment for {query.Split(' ')[2]}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Reset warning for {query}: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public async Task ExecuteQueryWithTestDataAsync_MultipleRuns_ShouldAlwaysBeUnique()
    {
        var request = new QueryExecutionRequest
        {
            SqlQuery = VOWIS_BUSINESS_SQL,
            DatabaseType = "MySQL",
            ConnectionString = REAL_MYSQL_CONNECTION,
            DesiredRecordCount = 8,
            UseAI = true
        };

        Console.WriteLine("🔄 Testing multiple runs for consistency...");

        var runResults = new List<(int run, bool success, string? error, int duplicates)>();

        for (int run = 1; run <= 3; run++)
        {
            Console.WriteLine($"\n=== RUN {run} ===");
            
            // 🧹 CLEANUP before each run to ensure clean state
            try
            {
                Console.WriteLine($"🧹 Cleaning up before run {run}...");
                await CleanupDatabase();
                Console.WriteLine($"✅ Cleanup completed for run {run}");
            }
            catch (Exception cleanupEx)
            {
                Console.WriteLine($"⚠️ Cleanup warning for run {run}: {cleanupEx.Message}");
                // Continue with test even if cleanup fails
            }
            
            try
            {
                var result = await _engineService.ExecuteQueryWithTestDataAsync(request);
                
                if (result.Success)
                {
                    var userRoleStatements = result.GeneratedInserts
                        .Where(sql => sql.ToLower().Contains("user_roles"))
                        .ToList();

                    var combinations = userRoleStatements
                        .Select(sql => ExtractUserRoleCombination(sql))
                        .ToList();

                    var duplicateCount = combinations.Count - combinations.Distinct().Count();
                    
                    Console.WriteLine($"  Generated {combinations.Count} user_roles, {duplicateCount} duplicates");
                    Console.WriteLine($"  Combinations: {string.Join(", ", combinations)}");
                    
                    runResults.Add((run, true, null, duplicateCount));
                }
                else
                {
                    Console.WriteLine($"  ❌ Failed: {result.ErrorMessage}");
                    runResults.Add((run, false, result.ErrorMessage, 0));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  🚨 Exception: {ex.Message}");
                var duplicateCount = ex.Message.Contains("Duplicate entry") ? 1 : 0;
                runResults.Add((run, false, ex.Message, duplicateCount));
            }
        }

        var successfulRuns = runResults.Count(r => r.success);
        var totalDuplicates = runResults.Sum(r => r.duplicates);
        
        Console.WriteLine($"\n📊 SUMMARY: {successfulRuns}/3 successful, {totalDuplicates} duplicates");

        Assert.AreEqual(3, successfulRuns, "All runs should succeed without duplicate key errors");
        Assert.AreEqual(0, totalDuplicates, "No duplicate combinations should be generated");

        Console.WriteLine("✅ CONSISTENCY TEST PASSED!");
    }

    [TestMethod]
    public async Task TestConnection_MySQL_ShouldConnect()
    {
        Console.WriteLine("🔌 Testing MySQL connection...");
        
        try
        {
            var connectionResult = await _engineService.TestConnectionAsync("MySQL", REAL_MYSQL_CONNECTION);

            Assert.IsTrue(connectionResult, 
                $"MySQL connection should succeed");

            Console.WriteLine("✅ MySQL connection successful!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MySQL connection failed: {ex.Message}");
            Assert.Fail($"Could not connect to MySQL database: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task Debug_MySQLColumnTypes_ShouldShowActualDataTypes()
    {
        try
        {
            using var connection = new MySqlConnector.MySqlConnection(REAL_MYSQL_CONNECTION);
            await connection.OpenAsync();

            // Query to see actual column types from MySQL schema
            var query = @"
                SELECT 
                    COLUMN_NAME,
                    DATA_TYPE,
                    COLUMN_TYPE,
                    IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'users'
                    AND TABLE_SCHEMA = DATABASE()
                    AND COLUMN_NAME IN ('date_of_birth', 'hire_date', 'last_login_at', 'created_at', 'updated_at')
                ORDER BY ORDINAL_POSITION";

            using var command = new MySqlConnector.MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            Console.WriteLine("=== MySQL Column Types Debug ===");
            while (await reader.ReadAsync())
            {
                var columnName = reader.GetString("COLUMN_NAME");
                var dataType = reader.GetString("DATA_TYPE");
                var columnType = reader.GetString("COLUMN_TYPE");
                var isNullable = reader.GetString("IS_NULLABLE");

                Console.WriteLine($"Column: {columnName}");
                Console.WriteLine($"  DATA_TYPE: '{dataType}'");
                Console.WriteLine($"  COLUMN_TYPE: '{columnType}'");
                Console.WriteLine($"  IS_NULLABLE: {isNullable}");
                Console.WriteLine();
            }

            Assert.IsTrue(true, "Debug test completed - check console output");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Debug test failed: {ex.Message}");
            throw;
        }
    }

    private static string ExtractUserRoleCombination(string insertSQL)
    {
        var valuesMatch = System.Text.RegularExpressions.Regex.Match(
            insertSQL, 
            @"VALUES\s*\((\d+),\s*(\d+)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (valuesMatch.Success)
        {
            var userId = valuesMatch.Groups[1].Value;
            var roleId = valuesMatch.Groups[2].Value;
            return $"{userId}-{roleId}";
        }

        return "unknown";
    }
} 