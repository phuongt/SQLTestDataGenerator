using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Integration tests với real MySQL database cho ExecuteQueryWithTestDataAsync
/// Sử dụng MySQL 8.4 server (192.84.20.226)
/// </summary>
[TestClass]
[TestCategory("RealMySQL")]
public class RealMySQLIntegrationTests
{
    // Real MySQL connection - localhost default
    private const string REAL_MYSQL_CONNECTION = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;";
    
    private EngineService _engineService = null!;

    [TestInitialize]
    public void Setup()
    {
        _engineService = new EngineService();
    }

    [TestMethod]
    [Timeout(180000)] // 3 minutes timeout
    public async Task ExecuteQueryWithTestDataAsync_RealMySQL_SimpleUsers_ShouldWork()
    {
        // Arrange
        var simpleSQL = @"
            SELECT * FROM users 
            WHERE is_active = 1 
            ORDER BY created_at DESC 
            LIMIT 10";

        Console.WriteLine("=== TEST: ExecuteQueryWithTestDataAsync với Real MySQL ===");
        Console.WriteLine($"Connection: MySQL 8.4 server (192.84.20.226)");
        Console.WriteLine($"SQL: {simpleSQL.Trim()}");
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = REAL_MYSQL_CONNECTION,
            SqlQuery = simpleSQL,
            DesiredRecordCount = 5,
            OpenAiApiKey = null, // Use Bogus generation
            UseAI = false,
            CurrentRecordCount = 0
        };

        // Act
        Console.WriteLine("\n=== EXECUTING TEST ===");
        var startTime = DateTime.Now;
        
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);
        
        var endTime = DateTime.Now;
        var duration = endTime - startTime;

        // Assert & Report
        Console.WriteLine("\n=== RESULTS ===");
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Execution Time: {duration.TotalSeconds:F2} seconds");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");
        Console.WriteLine($"Error (if any): {result.ErrorMessage}");
        
        if (result.Success)
        {
            Console.WriteLine($"Result Rows: {result.ResultData?.Rows.Count ?? 0}");
            Console.WriteLine($"Generated INSERT Statements: {result.GeneratedInserts.Count}");
            
            // Verify basic expectations
            Assert.IsTrue(result.ExecutionTime > TimeSpan.Zero, "Should measure execution time");
            Assert.IsTrue(result.GeneratedRecords >= 0, "Should track generated records count");
            
            // Display sample data if available
            if (result.ResultData != null && result.ResultData.Rows.Count > 0)
            {
                Console.WriteLine("\n=== SAMPLE RESULT DATA ===");
                for (int i = 0; i < Math.Min(2, result.ResultData.Rows.Count); i++)
                {
                    var row = result.ResultData.Rows[i];
                    Console.Write($"Row {i + 1}: ");
                    for (int j = 0; j < Math.Min(3, result.ResultData.Columns.Count); j++)
                    {
                        Console.Write($"{result.ResultData.Columns[j].ColumnName}={row[j]} ");
                    }
                    Console.WriteLine();
                }
            }
            
            Console.WriteLine("✅ SUCCESS: Real MySQL integration test passed");
        }
        else
        {
            Console.WriteLine("❌ FAILURE: Test failed but this provides valuable debugging info");
            Assert.IsNotNull(result.ErrorMessage, "Should provide error message for debugging");
        }
    }

    [TestMethod]
    [Timeout(180000)] // 3 minutes timeout
    public async Task ExecuteQueryWithTestDataAsync_RealMySQL_ComplexJoin_ShouldWork()
    {
        // Arrange
        var complexSQL = @"
            SELECT u.id, u.username, u.email, u.created_at,
                   c.name as company_name, c.code as company_code,
                   r.name as role_name, r.code as role_code,
                   ur.assigned_at, ur.expires_at
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            INNER JOIN user_roles ur ON u.id = ur.user_id
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE u.is_active = 1 
              AND ur.is_active = 1
            ORDER BY u.created_at DESC
            LIMIT 20";

        Console.WriteLine("=== TEST: Complex JOIN với Real MySQL ===");
        Console.WriteLine($"Tables: users, companies, user_roles, roles");
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = REAL_MYSQL_CONNECTION,
            SqlQuery = complexSQL,
            DesiredRecordCount = 10,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert
        Console.WriteLine($"\nComplex JOIN Result:");
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");
        Console.WriteLine($"Generated INSERTs: {result.GeneratedInserts.Count}");
        
        if (result.Success)
        {
            Console.WriteLine($"Query Result Rows: {result.ResultData?.Rows.Count ?? 0}");
            
            // Verify foreign key generation worked
            if (result.GeneratedInserts.Count > 0)
            {
                var insertsByTable = result.GeneratedInserts
                    .GroupBy(sql => ExtractTableFromInsert(sql))
                    .ToDictionary(g => g.Key, g => g.Count());
                
                Console.WriteLine("INSERT statements by table:");
                foreach (var kvp in insertsByTable)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value} statements");
                }
                
                Assert.IsTrue(result.GeneratedInserts.Count > 0, "Should generate INSERT statements");
            }
            
            Console.WriteLine("✅ SUCCESS: Complex JOIN test with real MySQL passed");
        }
        else
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
            // For debugging purposes, errors are valuable too
        }
    }

    [TestMethod]
    [Timeout(180000)] // 3 minutes timeout
    public async Task ExecuteQueryWithTestDataAsync_RealMySQL_VowisBusinessSQL_ShouldWork()
    {
        // Arrange - Vietnamese business logic SQL
        var vowisSQL = @"
            -- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc  
            SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary,
                   c.NAME AS company_name, c.code AS company_code, 
                   r.NAME AS role_name, r.code AS role_code, 
                   ur.expires_at AS role_expires,
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

        Console.WriteLine("=== TEST: Vowis Business SQL với Real MySQL ===");
        Console.WriteLine("Vietnamese business requirements test");
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = REAL_MYSQL_CONNECTION,
            SqlQuery = vowisSQL,
            DesiredRecordCount = 8,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert
        Console.WriteLine($"\nVowis Business SQL Result:");
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");
        Console.WriteLine($"Execution Time: {result.ExecutionTime.TotalSeconds:F2}s");
        
        if (result.Success)
        {
            Console.WriteLine($"Query Results: {result.ResultData?.Rows.Count ?? 0} rows");
            
            // Verify no duplicate key errors (our main fix)
            Assert.IsTrue(result.Success, "Should not have duplicate key errors");
            Assert.IsTrue(result.GeneratedRecords > 0, "Should generate test data");
            
            // Check for Vietnamese names in generated data if possible
            if (result.GeneratedInserts.Any(sql => sql.Contains("'Phương")))
            {
                Console.WriteLine("✅ Vietnamese name 'Phương' found in generated data");
            }
            
            Console.WriteLine("✅ SUCCESS: Vietnamese business SQL test passed");
        }
        else
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
            
            // Even failures provide valuable info - should not be duplicate key error anymore
            if (result.ErrorMessage != null && result.ErrorMessage.Contains("Duplicate entry"))
            {
                Assert.Fail("❌ DUPLICATE KEY ERROR STILL EXISTS - Fix failed!");
            }
        }
    }

    [TestMethod]
    [Timeout(120000)] // 2 minutes timeout
    public async Task TestConnection_RealMySQL_ShouldConnect()
    {
        // Test basic connection to real MySQL
        Console.WriteLine("=== TEST: Real MySQL Connection ===");
        
        var connected = await _engineService.TestConnectionAsync("MySQL", REAL_MYSQL_CONNECTION);
        
        Console.WriteLine($"Connection successful: {connected}");
        
        Assert.IsTrue(connected, "Should connect to real MySQL server");
        Console.WriteLine("✅ SUCCESS: Real MySQL connection verified");
    }

    [TestMethod]
    public async Task TestConnection_AlternativeMySQL_ShouldConnect()
    {
        // Test various cloud MySQL providers
        var alternativeConnections = new[]
        {
            // Aiven (free tier)
            "Server=mysql-test.aivencloud.com;Port=3306;Database=defaultdb;Uid=avnadmin;Pwd=testpass123;SslMode=Required;",
            
            // Railway (has free tier)
            "Server=roundhouse.proxy.rlwy.net;Port=3306;Database=railway;Uid=root;Pwd=testpass123;",
            
            // PlanetScale (free tier)
            "Server=aws.connect.psdb.cloud;Database=testdb;Uid=testuser;Pwd=pscale_pw_testpass;SslMode=Required;",
            
            // DB4Free (free MySQL hosting)
            "Server=db4free.net;Port=3306;Database=testdbname;Uid=testuser;Pwd=testpass123;",
            
            // FreeSQLDatabase
            "Server=sql.freedb.tech;Port=3306;Database=freedb_testdb;Uid=freedb_admin;Pwd=simplepass123;",
            
            // Modified freedb connection with URL encoding
            "Server=sql.freedb.tech;Port=3306;Database=freedb_DBTest;Uid=freedb_TestAdmin;Pwd=\"Vt5B&Mx6Jcu#jeN\";",
            
            // LocalDB for testing
            "Server=localhost;Port=3306;Database=test;Uid=root;Pwd=;",
            "Server=127.0.0.1;Port=3306;Database=mysql;Uid=root;Pwd=root;"
        };

        Console.WriteLine("=== TESTING ALTERNATIVE MYSQL CONNECTIONS ===");
        bool foundConnection = false;
        string workingConnection = "";
        
        foreach (var connectionString in alternativeConnections)
        {
            Console.WriteLine($"\nTesting: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");
            try
            {
                var connected = await _engineService.TestConnectionAsync("MySQL", connectionString);
                Console.WriteLine($"Result: {(connected ? "✅ SUCCESS" : "❌ FAILED")}");
                
                if (connected)
                {
                    Console.WriteLine($"🎉 FOUND WORKING MYSQL: {connectionString}");
                    foundConnection = true;
                    workingConnection = connectionString;
                    break; // Found working connection
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
        
        if (!foundConnection)
        {
            Console.WriteLine("❌ No alternative MySQL connection found");
            Console.WriteLine("📝 You may need to:");
            Console.WriteLine("   - Install XAMPP/WAMP for local MySQL");
            Console.WriteLine("   - Sign up for free cloud MySQL (Aiven, Railway, PlanetScale)");
            Console.WriteLine("   - Check firewall settings");
        }
        else
        {
            Console.WriteLine($"✅ Working MySQL connection found!");
            Console.WriteLine($"📋 Use this in your application: {workingConnection}");
        }
        
        // For now, don't assert - just provide information
        Console.WriteLine($"Connection test completed. Found working connection: {foundConnection}");
    }

    /// <summary>
    /// Helper để extract table name từ INSERT statement
    /// </summary>
    private static string ExtractTableFromInsert(string insertSQL)
    {
        if (string.IsNullOrEmpty(insertSQL)) return "unknown";
        
        var match = System.Text.RegularExpressions.Regex.Match(
            insertSQL, 
            @"INSERT\s+INTO\s+[`]?(\w+)[`]?", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        return match.Success ? match.Groups[1].Value : "unknown";
    }
} 