using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using System.Threading.Tasks;
using System.Configuration;

namespace SqlTestDataGenerator.Tests;

[TestClass]
public class RecordCountVerificationTests
{
    private static string TestConnectionString => 
        ConfigurationManager.AppSettings["DefaultConnectionString"] ?? 
        "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;";
    
    [TestMethod]
    [TestCategory("RecordCount")]
    [Timeout(180000)] // 3 minutes timeout
    public async Task ExecuteQueryWithTestDataAsync_RequestedRecordCount_ShouldGenerateCorrectAmountOfData()
    {
        // Arrange
        var engineService = new EngineService(DatabaseType.MySQL, TestConnectionString);
        var request = new QueryExecutionRequest
        {
            SqlQuery = @"
                SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth
                FROM users u 
                INNER JOIN companies c ON u.company_id = c.id 
                WHERE u.first_name LIKE '%John%' OR u.first_name LIKE '%Phương%' OR u.first_name LIKE '%Test%'",
            DatabaseType = "MySQL",
            ConnectionString = TestConnectionString,
            DesiredRecordCount = 8,  // Request exactly 8 records
            UseAI = false
        };
        
        // Act
        var result = await engineService.ExecuteQueryWithTestDataAsync(request);
        
        // Assert
        Assert.IsTrue(result.Success, $"Execution should succeed. Error: {result.ErrorMessage}");
        
        // Verify INSERT statements generated - should be baseRecordCount (10) per table for 2 tables = 20 total
        Assert.IsTrue(result.GeneratedInserts.Count > 0, "Should generate INSERT statements");
        Console.WriteLine($"📊 Generated {result.GeneratedInserts.Count} total INSERT statements");
        
        // Verify exact record count was generated
        Assert.AreEqual(request.DesiredRecordCount, result.GeneratedRecords, 
            $"Generated records ({result.GeneratedRecords}) MUST exactly match desired count ({request.DesiredRecordCount})");
        Console.WriteLine($"📊 Inserted {result.GeneratedRecords} records into database");
        
        // Verify query execution
        Assert.IsNotNull(result.ResultData, "Should return result data");
        Console.WriteLine($"📊 Query returned {result.ResultData.Rows.Count} result rows");
        Console.WriteLine($"📊 Execution time: {result.ExecutionTime.TotalMilliseconds}ms");
        
        // Key assertion: With smart data generation, we should get some matching records
        // The query has broad WHERE conditions (John OR Phương OR Test) so should match some records
        Assert.IsTrue(result.ResultData.Rows.Count > 0, 
            $"Query should return some matching records. Got {result.ResultData.Rows.Count} rows. " +
            $"Check if smart data generation is working properly.");
        
        // Verify reasonable execution time
        Assert.IsTrue(result.ExecutionTime.TotalSeconds < 30, 
            $"Execution should complete within 30 seconds. Took {result.ExecutionTime.TotalSeconds}s");
    }
    
    [TestMethod]
    [TestCategory("RecordCount")]
    [Timeout(180000)] // 3 minutes timeout
    public async Task ExecuteQueryWithTestDataAsync_SmallRecordCount_ShouldRespectMinimumRecords()
    {
        // Arrange
        var engineService = new EngineService(DatabaseType.MySQL, TestConnectionString);
        var request = new QueryExecutionRequest
        {
            SqlQuery = @"SELECT c.id, c.name, c.code FROM companies c WHERE c.name LIKE '%Test%'",
            DatabaseType = "MySQL",
            ConnectionString = TestConnectionString,
            DesiredRecordCount = 3,  // Small number
            UseAI = false
        };
        
        // Act
        var result = await engineService.ExecuteQueryWithTestDataAsync(request);
        
        // Assert
        Assert.IsTrue(result.Success, $"Execution should succeed. Error: {result.ErrorMessage}");
        
        // For single table with small record count, should generate at least the baseRecordCount (10)
        Console.WriteLine($"📊 Generated {result.GeneratedInserts.Count} INSERT statements for single table");
        Console.WriteLine($"📊 Query returned {result.ResultData.Rows.Count} rows");
        
        // Should execute successfully with exact count
        Assert.IsNotNull(result.ResultData, "Should return result data");
        Assert.AreEqual(request.DesiredRecordCount, result.GeneratedRecords, 
            $"Generated records ({result.GeneratedRecords}) MUST exactly match desired count ({request.DesiredRecordCount})");
    }
    
    [TestMethod] 
    [TestCategory("RecordCount")]
    [Timeout(180000)] // 3 minutes timeout
    public async Task ExecuteQueryWithTestDataAsync_LargeRecordCount_ShouldHandleEfficiently()
    {
        // Arrange
        var engineService = new EngineService(DatabaseType.MySQL, TestConnectionString);
        var request = new QueryExecutionRequest
        {
            SqlQuery = @"SELECT u.id, u.username, u.email FROM users u WHERE u.email IS NOT NULL",
            DatabaseType = "MySQL", 
            ConnectionString = TestConnectionString,
            DesiredRecordCount = 25,  // Larger number
            UseAI = false
        };
        
        // Act
        var result = await engineService.ExecuteQueryWithTestDataAsync(request);
        
        // Assert
        Assert.IsTrue(result.Success, $"Execution should succeed. Error: {result.ErrorMessage}");
        
        Console.WriteLine($"📊 Generated {result.GeneratedInserts.Count} INSERT statements for large count");
        Console.WriteLine($"📊 Query returned {result.ResultData.Rows.Count} rows");
        
        // Should handle large counts efficiently
        Assert.IsTrue(result.ExecutionTime.TotalSeconds < 45, 
            $"Should handle large counts efficiently. Took {result.ExecutionTime.TotalSeconds}s");
        
        // Should generate exact data count
        Assert.AreEqual(request.DesiredRecordCount, result.GeneratedRecords, 
            $"Generated records ({result.GeneratedRecords}) MUST exactly match desired count ({request.DesiredRecordCount})");
    }
} 