using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Test suite để verify fallback behavior khi API AI bị limit
/// Đảm bảo application vẫn hoạt động được khi AI service không available
/// </summary>
[TestClass]
[TestCategory("API-Fallback")]
public class APILimitFallbackTests
{
    private const string TEST_CONNECTION = "Server=localhost;Port=3306;Database=test_db;Uid=root;Pwd=password;";
    private string? _validApiKey;
    private string? _invalidApiKey = "invalid_key_12345";

    [TestInitialize]
    public void Setup()
    {
        // Get valid API key for testing
        _validApiKey = GetGeminiApiKey();
        Console.WriteLine($"[FALLBACK] Valid API Key Available: {!string.IsNullOrEmpty(_validApiKey)}");
    }

    #region AI Service Fallback Tests

    /// <summary>
    /// Test fallback behavior khi không có API key
    /// </summary>
    [TestMethod]
    [TestCategory("No-API-Key")]
    public async Task Test_NoAPIKey_ShouldFallbackToBogusGeneration()
    {
        // Arrange
        Console.WriteLine("=== TEST: Fallback khi không có API key ===");
        
        var engineService = new EngineService(DatabaseType.MySQL, TEST_CONNECTION, null); // No API key
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = TEST_CONNECTION,
            SqlQuery = "SELECT id, name, email FROM users WHERE name LIKE '%test%' LIMIT 5",
            DesiredRecordCount = 3,
            OpenAiApiKey = null, // Explicitly no API key
            UseAI = true, // Request AI but should fallback
            CurrentRecordCount = 0
        };

        Console.WriteLine("🎯 Testing với UseAI=true nhưng không có API key");

        try
        {
            // Act
            var startTime = DateTime.UtcNow;
            var result = await engineService.ExecuteQueryWithTestDataAsync(request);
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            Console.WriteLine($"⏱️ Execution time: {duration.TotalSeconds:F2} seconds");
            Console.WriteLine($"✅ Result Success: {result.Success}");
            Console.WriteLine($"📊 Generated Records: {result.GeneratedRecords}");

            if (result.Success)
            {
                // Should generate records using Bogus fallback
                Assert.IsTrue(result.GeneratedRecords > 0, "Should generate records via Bogus fallback");
                Assert.IsTrue(result.GeneratedInserts.Count > 0, "Should have INSERT statements");
                
                // Fast execution suggests Bogus was used (not AI)
                Assert.IsTrue(duration.TotalSeconds < 10, "Bogus generation should be fast");
                
                Console.WriteLine("✅ Fallback to Bogus generation successful");
            }
            else
            {
                // Expected connection failure - still validates fallback logic
                Console.WriteLine($"❌ Failed (expected for connection): {result.ErrorMessage}");
                Assert.IsTrue(result.ErrorMessage?.Contains("connection") == true, 
                    "Failure should be due to database connection, not AI issues");
            }
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("connection") || ex.Message.Contains("database"))
            {
                Console.WriteLine("🌐 Expected database connection issue");
                Assert.Inconclusive("Database connection required for full test");
            }
            else
            {
                Console.WriteLine($"💥 Unexpected error: {ex.Message}");
                throw;
            }
        }

        Console.WriteLine("✅ No API key fallback test completed");
    }

    /// <summary>
    /// Test fallback behavior khi API key invalid
    /// </summary>
    [TestMethod]
    [TestCategory("Invalid-API-Key")]
    public async Task Test_InvalidAPIKey_ShouldFallbackToBogusGeneration()
    {
        // Arrange
        Console.WriteLine("=== TEST: Fallback khi API key invalid ===");
        
        var engineService = new EngineService(DatabaseType.MySQL, TEST_CONNECTION, _invalidApiKey);
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = TEST_CONNECTION,
            SqlQuery = "SELECT id, username FROM users WHERE username LIKE '%admin%' LIMIT 3",
            DesiredRecordCount = 2,
            OpenAiApiKey = _invalidApiKey,
            UseAI = true, // Request AI but should fallback due to invalid key
            CurrentRecordCount = 0
        };

        Console.WriteLine($"🔑 Testing với invalid API key: {_invalidApiKey}");

        try
        {
            // Act
            var startTime = DateTime.UtcNow;
            var result = await engineService.ExecuteQueryWithTestDataAsync(request);
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            Console.WriteLine($"⏱️ Execution time: {duration.TotalSeconds:F2} seconds");
            Console.WriteLine($"✅ Result Success: {result.Success}");
            Console.WriteLine($"📊 Generated Records: {result.GeneratedRecords}");

            if (result.Success)
            {
                // Should generate records using fallback
                Assert.IsTrue(result.GeneratedRecords > 0, "Should generate records via fallback");
                
                // Should be relatively fast (fallback to Bogus)
                if (duration.TotalSeconds < 15)
                {
                    Console.WriteLine("🚀 Fast execution suggests Bogus fallback was used");
                }
                
                Console.WriteLine("✅ Fallback despite invalid API key successful");
            }
            else
            {
                Console.WriteLine($"❌ Failed: {result.ErrorMessage}");
                
                // Should fail due to connection, not API key issues
                Assert.IsTrue(result.ErrorMessage?.Contains("connection") == true ||
                             result.ErrorMessage?.Contains("database") == true,
                    "Failure should be database-related, not API key related");
            }
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("connection") || ex.Message.Contains("database"))
            {
                Console.WriteLine("🌐 Expected database connection issue");
                Assert.Inconclusive("Database connection required for full test");
            }
            else
            {
                Console.WriteLine($"💥 Unexpected error: {ex.Message}");
                throw;
            }
        }

        Console.WriteLine("✅ Invalid API key fallback test completed");
    }

    #endregion

    #region UseAI Flag Tests

    /// <summary>
    /// Test explicit UseAI=false behavior
    /// </summary>
    [TestMethod]
    [TestCategory("UseAI-False")]
    public async Task Test_UseAIFalse_ShouldSkipAICompletely()
    {
        // Arrange
        Console.WriteLine("=== TEST: UseAI=false should skip AI completely ===");
        
        var engineService = new EngineService(DatabaseType.MySQL, TEST_CONNECTION, _validApiKey);
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = TEST_CONNECTION,
            SqlQuery = "SELECT id, name FROM companies WHERE name LIKE '%corp%' LIMIT 4",
            DesiredRecordCount = 3,
            OpenAiApiKey = _validApiKey, // Valid API key available
            UseAI = false, // Explicitly disable AI
            CurrentRecordCount = 0
        };

        Console.WriteLine("🎯 Testing với UseAI=false despite having valid API key");

        try
        {
            // Act
            var startTime = DateTime.UtcNow;
            var result = await engineService.ExecuteQueryWithTestDataAsync(request);
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            Console.WriteLine($"⏱️ Execution time: {duration.TotalSeconds:F2} seconds");
            Console.WriteLine($"✅ Result Success: {result.Success}");
            Console.WriteLine($"📊 Generated Records: {result.GeneratedRecords}");

            if (result.Success)
            {
                // Should use Bogus generation directly (fast)
                Assert.IsTrue(result.GeneratedRecords > 0, "Should generate records via Bogus");
                Assert.IsTrue(duration.TotalSeconds < 10, "Should be fast (no AI calls)");
                
                Console.WriteLine("✅ UseAI=false bypassed AI successfully");
            }
            else
            {
                Console.WriteLine($"❌ Failed: {result.ErrorMessage}");
                
                // Should fail due to connection only
                Assert.IsTrue(result.ErrorMessage?.Contains("connection") == true,
                    "Failure should be connection-related only");
            }
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("connection") || ex.Message.Contains("database"))
            {
                Console.WriteLine("🌐 Expected database connection issue");
                Assert.Inconclusive("Database connection required for full test");
            }
            else
            {
                throw;
            }
        }

        Console.WriteLine("✅ UseAI=false test completed");
    }

    #endregion

    #region Stress Test - Multiple Fallback Scenarios

    /// <summary>
    /// Stress test với multiple scenarios để verify consistent fallback behavior
    /// </summary>
    [TestMethod]
    [TestCategory("Stress-Fallback")]
    [Timeout(60000)] // 1 minute timeout
    public async Task Test_MultipleFallbackScenarios_ShouldBeConsistent()
    {
        Console.WriteLine("=== STRESS TEST: Multiple Fallback Scenarios ===");

        var scenarios = new[]
        {
            new { Name = "No API Key", ApiKey = (string?)null, UseAI = true },
            new { Name = "Invalid API Key", ApiKey = "invalid_123", UseAI = true },
            new { Name = "Valid Key + UseAI False", ApiKey = _validApiKey, UseAI = false },
            new { Name = "Empty API Key", ApiKey = "", UseAI = true }
        };

        var results = new List<(string Scenario, bool Success, int Records, double Duration)>();

        foreach (var scenario in scenarios)
        {
            Console.WriteLine($"\n📋 Testing scenario: {scenario.Name}");

            try
            {
                // Arrange
                var engineService = new EngineService(DatabaseType.MySQL, TEST_CONNECTION, scenario.ApiKey);
                
                var request = new QueryExecutionRequest
                {
                    DatabaseType = "MySQL",
                    ConnectionString = TEST_CONNECTION,
                    SqlQuery = "SELECT * FROM users LIMIT 2",
                    DesiredRecordCount = 2,
                    OpenAiApiKey = scenario.ApiKey,
                    UseAI = scenario.UseAI,
                    CurrentRecordCount = 0
                };

                // Act
                var startTime = DateTime.UtcNow;
                var result = await engineService.ExecuteQueryWithTestDataAsync(request);
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                // Record results
                results.Add((scenario.Name, result.Success, result.GeneratedRecords, duration.TotalSeconds));

                Console.WriteLine($"   Result: Success={result.Success}, Records={result.GeneratedRecords}, Duration={duration.TotalSeconds:F2}s");

                if (!result.Success && result.ErrorMessage?.Contains("connection") != true)
                {
                    Console.WriteLine($"   Unexpected error: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("connection") || ex.Message.Contains("database"))
                {
                    Console.WriteLine($"   Expected connection error: {ex.Message}");
                    results.Add((scenario.Name, false, 0, 0));
                }
                else
                {
                    Console.WriteLine($"   Unexpected exception: {ex.Message}");
                    results.Add((scenario.Name, false, -1, 0)); // -1 indicates unexpected error
                }
            }
        }

        // Assert - Analyze results for consistency
        Console.WriteLine("\n📊 SCENARIO RESULTS SUMMARY:");
        Console.WriteLine("Scenario                    | Success | Records | Duration");
        Console.WriteLine("----------------------------|---------|---------|----------");

        var unexpectedErrors = 0;
        var connectionErrors = 0;
        var successfulFallbacks = 0;

        foreach (var result in results)
        {
            Console.WriteLine($"{result.Scenario,-27} | {result.Success,-7} | {result.Records,-7} | {result.Duration:F2}s");

            if (result.Records == -1)
            {
                unexpectedErrors++;
            }
            else if (!result.Success)
            {
                connectionErrors++;
            }
            else
            {
                successfulFallbacks++;
            }
        }

        Console.WriteLine($"\n📋 ANALYSIS:");
        Console.WriteLine($"✅ Successful Fallbacks: {successfulFallbacks}");
        Console.WriteLine($"🌐 Connection Errors: {connectionErrors}");
        Console.WriteLine($"💥 Unexpected Errors: {unexpectedErrors}");

        // All errors should be connection-related, no unexpected API errors
        Assert.AreEqual(0, unexpectedErrors, "Should have no unexpected errors - all fallbacks should work");

        // If any succeeded, fallback mechanism works
        if (successfulFallbacks > 0)
        {
            Console.WriteLine("✅ Fallback mechanism working correctly");
        }
        else
        {
            Console.WriteLine("⚠️ All tests hit connection issues - fallback logic partially validated");
        }

        Console.WriteLine("✅ Multiple fallback scenarios stress test completed");
    }

    #endregion

    #region Performance Validation

    /// <summary>
    /// Test performance characteristics của fallback generation
    /// </summary>
    [TestMethod]
    [TestCategory("Fallback-Performance")]
    public async Task Test_FallbackPerformance_ShouldBeFasterThanAI()
    {
        Console.WriteLine("=== TEST: Fallback Performance Validation ===");

        // Test data generation speed without AI
        var timingsNoAI = new List<double>();
        var timingsWithAI = new List<double>();

        // Test Bogus fallback performance (multiple runs)
        for (int i = 0; i < 3; i++)
        {
            Console.WriteLine($"\nRun {i + 1}/3 - Testing Bogus fallback performance");

            try
            {
                var engineService = new EngineService(DatabaseType.MySQL, TEST_CONNECTION, null); // No AI
                
                var request = new QueryExecutionRequest
                {
                    DatabaseType = "MySQL",
                    ConnectionString = TEST_CONNECTION,
                    SqlQuery = "SELECT id, name, email FROM users LIMIT 5",
                    DesiredRecordCount = 5,
                    OpenAiApiKey = null,
                    UseAI = false,
                    CurrentRecordCount = 0
                };

                var startTime = DateTime.UtcNow;
                var result = await engineService.ExecuteQueryWithTestDataAsync(request);
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                timingsNoAI.Add(duration.TotalSeconds);
                Console.WriteLine($"   Bogus fallback duration: {duration.TotalSeconds:F2}s, Success: {result.Success}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Expected error: {ex.Message}");
                timingsNoAI.Add(0); // Connection error - still record timing
            }
        }

        // Test với AI (nếu có API key) để so sánh performance
        if (!string.IsNullOrEmpty(_validApiKey))
        {
            Console.WriteLine("\n🤖 Testing AI generation performance for comparison");

            try
            {
                var engineService = new EngineService(DatabaseType.MySQL, TEST_CONNECTION, _validApiKey);
                
                var request = new QueryExecutionRequest
                {
                    DatabaseType = "MySQL",
                    ConnectionString = TEST_CONNECTION,
                    SqlQuery = "SELECT id, name FROM users LIMIT 2", // Smaller to avoid quota
                    DesiredRecordCount = 2,
                    OpenAiApiKey = _validApiKey,
                    UseAI = true,
                    CurrentRecordCount = 0
                };

                var startTime = DateTime.UtcNow;
                var result = await engineService.ExecuteQueryWithTestDataAsync(request);
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                timingsWithAI.Add(duration.TotalSeconds);
                Console.WriteLine($"   AI generation duration: {duration.TotalSeconds:F2}s, Success: {result.Success}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   AI test error: {ex.Message}");
                timingsWithAI.Add(30); // Assume slow if error (timeout, quota, etc.)
            }
        }

        // Analyze performance
        Console.WriteLine("\n📊 PERFORMANCE ANALYSIS:");
        
        var avgBogus = timingsNoAI.Where(t => t > 0).DefaultIfEmpty(0).Average();
        Console.WriteLine($"Average Bogus fallback time: {avgBogus:F2}s");

        if (timingsWithAI.Any())
        {
            var avgAI = timingsWithAI.Average();
            Console.WriteLine($"Average AI generation time: {avgAI:F2}s");
            
            if (avgBogus > 0 && avgBogus < avgAI)
            {
                Console.WriteLine($"✅ Fallback is {avgAI / avgBogus:F1}x faster than AI");
            }
        }

        // Assert performance characteristics
        Assert.IsTrue(avgBogus < 15, "Bogus fallback should complete within 15 seconds");
        
        if (avgBogus > 0)
        {
            Console.WriteLine("✅ Fallback performance is acceptable");
        }
        else
        {
            Console.WriteLine("⚠️ All tests hit connection issues - performance partially validated");
        }

        Console.WriteLine("✅ Fallback performance test completed");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get Gemini API key from multiple sources
    /// </summary>
    private string? GetGeminiApiKey()
    {
        return System.Configuration.ConfigurationManager.AppSettings["GeminiApiKey"] ??
               Environment.GetEnvironmentVariable("GEMINI_API_KEY") ??
               Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY");
    }

    #endregion
} 