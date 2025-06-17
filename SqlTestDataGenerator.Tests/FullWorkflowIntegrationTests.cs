using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Services;
using System.Data;
using Dapper;
using MySqlConnector;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Integration tests for full workflow: Generate SQL → Export to File → Commit to Database
/// Tests the complete user journey from UI perspective
/// </summary>
[TestClass]
public class FullWorkflowIntegrationTests
{
    private EngineService? _engineService;
    private string _connectionString = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;";
    private const string DatabaseType = "MySQL";

    [TestInitialize]
    public void Setup()
    {
        Console.WriteLine("=== FullWorkflow Integration Test Setup ===");
        _engineService = new EngineService("AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM");
        
        // Ensure export directory exists
        var exportDir = "sql-exports";
        if (!Directory.Exists(exportDir))
        {
            Directory.CreateDirectory(exportDir);
            Console.WriteLine($"✅ Created export directory: {exportDir}");
        }
    }

    [TestCleanup]  
    public void Cleanup()
    {
        Console.WriteLine("=== FullWorkflow Integration Test Cleanup ===");
        _engineService = null;
    }

    /// <summary>
    /// TC_FULL_WORKFLOW_001: Test complete workflow with complex SQL
    /// 1. Generate complex test data with AI
    /// 2. Verify SQL file is exported
    /// 3. Execute SQL from file and commit to database
    /// 4. Verify data is persisted in database
    /// </summary>
    [TestMethod]
    public async Task TC_FULL_WORKFLOW_001_ComplexSQL_GenerateExportCommit()
    {
        // ARRANGE
        Console.WriteLine("🚀 TC_FULL_WORKFLOW_001: Testing complete Generate → Export → Commit workflow");
        
        Assert.IsNotNull(_engineService, "Engine service should be initialized");
        
        var complexSql = @"
            -- Complex SQL with multiple JOINs, WHERE conditions, and date functions
            SELECT 
                u.id, u.username, u.first_name, u.last_name, u.email,
                u.date_of_birth, u.salary, u.department, u.hire_date,
                c.name as company_name, c.code as company_code,
                r.name as role_name, r.code as role_code,
                ur.expires_at as role_expires,
                CASE 
                    WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                    WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn vai trò'
                    ELSE 'Đang làm việc'
                END as work_status
            FROM users u
            JOIN companies c ON u.company_id = c.id
            JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = TRUE
            JOIN roles r ON ur.role_id = r.id
            WHERE 
                (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
                AND YEAR(u.date_of_birth) = 1989
                AND c.name LIKE '%VNEXT%'
                AND r.code LIKE '%DD%'
                AND (u.is_active = 0 OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY))
            ORDER BY ur.expires_at ASC, u.created_at DESC
            LIMIT 5";

        var request = new QueryExecutionRequest
        {
            DatabaseType = DatabaseType,
            ConnectionString = _connectionString,
            SqlQuery = complexSql,
            DesiredRecordCount = 8,
            UseAI = true,
            OpenAiApiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM"
        };

        Console.WriteLine($"📝 Testing with SQL: {complexSql.Length} characters");
        Console.WriteLine($"🎯 Target records: {request.DesiredRecordCount}");

        // ACT PHASE 1: Generate Test Data with Export
        Console.WriteLine("\n=== PHASE 1: Generate Test Data & Export to File ===");
        
        var generateResult = await _engineService.ExecuteQueryWithTestDataAsync(request);
        
        // ASSERT PHASE 1: Verify Generation and Export
        Assert.IsTrue(generateResult.Success, $"Generation should succeed. Error: {generateResult.ErrorMessage}");
        
        // 1. Check số lượng record gen ra được = số lượng expect ban đầu
        Assert.AreEqual(request.DesiredRecordCount, generateResult.GeneratedRecords, 
            $"Generated records should match expected count. Expected: {request.DesiredRecordCount}, Actual: {generateResult.GeneratedRecords}");
        
        // 2. Check file SQL đã được tạo đúng tên đúng folder
        Assert.IsNotNull(generateResult.ExportedFilePath, "SQL file path should be exported");
        Assert.IsTrue(!string.IsNullOrEmpty(generateResult.ExportedFilePath), "Exported file path should not be empty");
        Assert.IsTrue(File.Exists(generateResult.ExportedFilePath), $"Exported SQL file should exist: {generateResult.ExportedFilePath}");
        Assert.IsTrue(generateResult.ExportedFilePath.Contains("sql-exports"), "SQL file should be in sql-exports folder");
        Assert.IsTrue(Path.GetFileName(generateResult.ExportedFilePath).StartsWith("generated_inserts_"), "SQL file should have correct naming pattern");
        
        Console.WriteLine($"✅ Generation completed successfully");
        Console.WriteLine($"📁 SQL file exported to: {generateResult.ExportedFilePath}");
        Console.WriteLine($"🔢 Generated {generateResult.GeneratedRecords} records (matches expected: {request.DesiredRecordCount})");
        Console.WriteLine($"📊 Query result: {generateResult.ResultData.Rows.Count} rows (rollback preview)");

        // Read SQL file for execution
        var sqlFileContent = await File.ReadAllTextAsync(generateResult.ExportedFilePath);
        var sqlStatements = sqlFileContent.Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries)
                                         .Where(s => !string.IsNullOrWhiteSpace(s) && s.Trim().StartsWith("INSERT"))
                                         .ToList();
        
        Console.WriteLine($"📄 SQL file contains {sqlStatements.Count} INSERT statements");

        // ACT PHASE 2: Execute SQL from File (Commit to Database)
        Console.WriteLine("\n=== PHASE 2: Execute SQL from File & Commit to Database ===");
        
        using var connection = DbConnectionFactory.CreateConnection(DatabaseType, _connectionString);
        connection.Open();
        
        // Clear existing data first
        Console.WriteLine("🧹 Clearing existing test data...");
        using var clearTransaction = connection.BeginTransaction();
        try
        {
            // Disable foreign key checks temporarily
            await connection.ExecuteAsync("SET foreign_key_checks = 0", transaction: clearTransaction);
            
            await connection.ExecuteAsync("TRUNCATE TABLE user_roles", transaction: clearTransaction);
            await connection.ExecuteAsync("TRUNCATE TABLE users", transaction: clearTransaction);
            await connection.ExecuteAsync("TRUNCATE TABLE companies", transaction: clearTransaction);
            await connection.ExecuteAsync("TRUNCATE TABLE roles", transaction: clearTransaction);
            
            // Re-enable foreign key checks
            await connection.ExecuteAsync("SET foreign_key_checks = 1", transaction: clearTransaction);
            
            clearTransaction.Commit();
            Console.WriteLine("✅ Existing data cleared");
        }
        catch
        {
            clearTransaction.Rollback();
            throw;
        }

        // Execute SQL statements from file
        using var commitTransaction = connection.BeginTransaction();
        int executedCount = 0;
        try
        {
            foreach (var sqlStatement in sqlStatements)
            {
                if (string.IsNullOrWhiteSpace(sqlStatement)) continue;
                
                var cleanSql = sqlStatement.Trim().TrimEnd(';') + ";";
                Console.WriteLine($"🔄 Executing: {cleanSql.Substring(0, Math.Min(80, cleanSql.Length))}...");
                
                await connection.ExecuteAsync(cleanSql, transaction: commitTransaction, commandTimeout: 300);
                executedCount++;
            }
            
            // Commit transaction
            commitTransaction.Commit();
            Console.WriteLine($"✅ Successfully committed {executedCount} SQL statements to database");
        }
        catch (Exception ex)
        {
            commitTransaction.Rollback();
            Console.WriteLine($"❌ Commit failed, transaction rolled back: {ex.Message}");
            throw;
        }

        // ASSERT PHASE 2: Verify Data Persistence
        Console.WriteLine("\n=== PHASE 3: Verify Data Persistence in Database ===");
        
        // 3. Check số lượng record insert được vào DB = số lượng expect ban đầu
        var userCount = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users");
        
        Console.WriteLine($"📊 Data in database after commit:");
        Console.WriteLine($"   - Users: {userCount}");
        Console.WriteLine($"   - Expected: {request.DesiredRecordCount}");
        
        Assert.AreEqual(request.DesiredRecordCount, userCount, 
            $"Inserted users count should match expected count. Expected: {request.DesiredRecordCount}, Actual: {userCount}");

        // FINAL SUCCESS
        Console.WriteLine("\n🎉 ===== FULL WORKFLOW TEST COMPLETED SUCCESSFULLY ===== 🎉");
        Console.WriteLine($"✅ Phase 1: Generated {generateResult.GeneratedRecords} records and exported to file");
        Console.WriteLine($"✅ Phase 2: Executed {executedCount} SQL statements and committed to database");  
        Console.WriteLine($"✅ Phase 3: Verified {userCount} users inserted to database matches expected {request.DesiredRecordCount}");
        Console.WriteLine($"📁 SQL File: {Path.GetFileName(generateResult.ExportedFilePath)}");
        Console.WriteLine("🔄 Complete workflow: Generate → Export → Commit → Verify ✅");
    }



    /// <summary>
    /// Helper method to create DataTable from dynamic query results
    /// </summary>
    private static DataTable CreateDataTable(IEnumerable<dynamic> data)
    {
        var dataTable = new DataTable();
        var items = data.ToList();
        
        if (!items.Any()) return dataTable;

        var first = items.First() as IDictionary<string, object>;
        if (first != null)
        {
            foreach (var key in first.Keys)
            {
                dataTable.Columns.Add(key);
            }

            foreach (var item in items)
            {
                var row = dataTable.NewRow();
                var dict = item as IDictionary<string, object>;
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        row[kvp.Key] = kvp.Value ?? DBNull.Value;
                    }
                }
                dataTable.Rows.Add(row);
            }
        }

        return dataTable;
    }
} 