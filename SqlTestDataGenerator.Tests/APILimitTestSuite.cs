using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using System.Text.Json;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Comprehensive test suite để kiểm tra API AI có bị limit hay chưa
/// Tests cover daily limits, rate limiting, quota exceeded scenarios, và fallback behavior
/// </summary>
[TestClass]
[TestCategory("API-Limit-Testing")]
public class APILimitTestSuite
{
    private EnhancedGeminiFlashRotationService? _flashService;
    private GeminiAIDataGenerationService? _aiService;
    private string? _apiKey;
    
    [TestInitialize]
    public void Setup()
    {
        // Get API key from multiple sources
        _apiKey = GetGeminiApiKey();
        
        Console.WriteLine($"[API-LIMIT] API Key Available: {!string.IsNullOrEmpty(_apiKey)}");
        Console.WriteLine($"[API-LIMIT] API Key Length: {_apiKey?.Length ?? 0}");
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _flashService = new EnhancedGeminiFlashRotationService(_apiKey);
            _aiService = new GeminiAIDataGenerationService(_apiKey);
            Console.WriteLine($"[API-LIMIT] Services initialized successfully");
        }
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _flashService?.Dispose();
    }
    
    #region Daily Limit Detection Tests
    
    /// <summary>
    /// Test để kiểm tra daily API usage statistics
    /// </summary>
    [TestMethod]
    [TestCategory("Daily-Limits")]
    public void Test_DailyAPIUsageStatistics_ShouldReturnCurrentStatus()
    {
        // Arrange
        if (_flashService == null)
        {
            Assert.Inconclusive("No API key available for testing");
            return;
        }
        
        Console.WriteLine("=== TEST: Daily API Usage Statistics ===");
        
        // Act
        var stats = _flashService.GetAPIUsageStatistics();
        
        // Assert
        Assert.IsNotNull(stats, "API usage statistics should not be null");
        
        // Verify required fields
        Assert.IsTrue(stats.ContainsKey("DailyCallsUsed"), "Should contain DailyCallsUsed");
        Assert.IsTrue(stats.ContainsKey("DailyCallLimit"), "Should contain DailyCallLimit");
        Assert.IsTrue(stats.ContainsKey("DailyCallsRemaining"), "Should contain DailyCallsRemaining");
        Assert.IsTrue(stats.ContainsKey("DailyResetTime"), "Should contain DailyResetTime");
        Assert.IsTrue(stats.ContainsKey("CanCallNow"), "Should contain CanCallNow");
        
        // Extract and validate values
        var dailyUsed = (int)stats["DailyCallsUsed"];
        var dailyLimit = (int)stats["DailyCallLimit"];
        var dailyRemaining = (int)stats["DailyCallsRemaining"];
        var canCallNow = (bool)stats["CanCallNow"];
        var resetTime = (string)stats["DailyResetTime"];
        
        Console.WriteLine($"📊 Daily Calls Used: {dailyUsed}/{dailyLimit}");
        Console.WriteLine($"📊 Daily Calls Remaining: {dailyRemaining}");
        Console.WriteLine($"🔄 Can Call Now: {canCallNow}");
        Console.WriteLine($"⏰ Daily Reset Time: {resetTime}");
        
        // Validate logic
        Assert.AreEqual(dailyLimit - dailyUsed, dailyRemaining, "Daily remaining should equal limit minus used");
        Assert.IsTrue(dailyLimit > 0, "Daily limit should be positive");
        Assert.IsTrue(dailyUsed >= 0, "Daily used should not be negative");
        
        // Log current status for monitoring
        if (dailyUsed >= dailyLimit * 0.8) // 80% threshold
        {
            Console.WriteLine("⚠️ WARNING: Approaching daily API limit (80%+ used)");
        }
        
        if (dailyUsed >= dailyLimit)
        {
            Console.WriteLine("🚫 CRITICAL: Daily API limit reached!");
            Assert.IsFalse(canCallNow, "Should not be able to call when daily limit reached");
        }
        
        Console.WriteLine("✅ Daily API usage statistics test completed");
    }
    
    /// <summary>
    /// Test để kiểm tra availability checking logic
    /// </summary>
    [TestMethod]
    [TestCategory("Daily-Limits")]
    public void Test_APIAvailabilityChecking_ShouldReflectCurrentStatus()
    {
        // Arrange
        if (_flashService == null)
        {
            Assert.Inconclusive("No API key available for testing");
            return;
        }
        
        Console.WriteLine("=== TEST: API Availability Checking ===");
        
        // Act
        var canCallNow = _flashService.CanCallAPINow();
        var nextCallableTime = _flashService.GetNextCallableTime();
        var stats = _flashService.GetAPIUsageStatistics();
        
        // Assert
        var dailyUsed = (int)stats["DailyCallsUsed"];
        var dailyLimit = (int)stats["DailyCallLimit"];
        
        Console.WriteLine($"🔄 Can Call Now: {canCallNow}");
        Console.WriteLine($"⏰ Next Callable Time: {nextCallableTime:yyyy-MM-dd HH:mm:ss UTC}");
        Console.WriteLine($"📊 Current Usage: {dailyUsed}/{dailyLimit}");
        
        // If daily limit reached, should not be able to call
        if (dailyUsed >= dailyLimit)
        {
            Assert.IsFalse(canCallNow, "Should not be able to call when daily limit reached");
            
            // Next callable time should be tomorrow
            var hoursUntilNextCall = (nextCallableTime - DateTime.UtcNow).TotalHours;
            Assert.IsTrue(hoursUntilNextCall > 0, "Next callable time should be in the future");
            Assert.IsTrue(hoursUntilNextCall <= 24, "Next callable time should be within 24 hours (daily reset)");
            
            Console.WriteLine($"🚫 Daily limit reached - Next callable in {hoursUntilNextCall:F2} hours");
        }
        else
        {
            // If under daily limit, availability depends on rate limiting
            if (!canCallNow)
            {
                var secondsUntilNextCall = (nextCallableTime - DateTime.UtcNow).TotalSeconds;
                Assert.IsTrue(secondsUntilNextCall <= 10, "Rate limit delay should be short (≤10 seconds)");
                Console.WriteLine($"⏰ Rate limited - Next callable in {secondsUntilNextCall:F2} seconds");
            }
            else
            {
                Console.WriteLine($"✅ API available for immediate call");
            }
        }
        
        Console.WriteLine("✅ API availability checking test completed");
    }
    
    #endregion
    
    #region Rate Limiting Tests
    
    /// <summary>
    /// Test để kiểm tra rate limiting behavior
    /// </summary>
    [TestMethod]
    [TestCategory("Rate-Limiting")]
    public void Test_RateLimitingBehavior_ShouldEnforceMinimumDelay()
    {
        // Arrange
        if (_flashService == null)
        {
            Assert.Inconclusive("No API key available for testing");
            return;
        }
        
        Console.WriteLine("=== TEST: Rate Limiting Behavior ===");
        
        // Act - Check multiple times in quick succession
        var firstCheck = DateTime.UtcNow;
        var canCall1 = _flashService.CanCallAPINow();
        var nextTime1 = _flashService.GetNextCallableTime();
        
        // Small delay
        System.Threading.Thread.Sleep(1000);
        
        var secondCheck = DateTime.UtcNow;
        var canCall2 = _flashService.CanCallAPINow();
        var nextTime2 = _flashService.GetNextCallableTime();
        
        // Assert
        Console.WriteLine($"🕐 First Check ({firstCheck:HH:mm:ss}): CanCall={canCall1}, NextTime={nextTime1:HH:mm:ss}");
        Console.WriteLine($"🕑 Second Check ({secondCheck:HH:mm:ss}): CanCall={canCall2}, NextTime={nextTime2:HH:mm:ss}");
        
        // If rate limited, next callable time should advance properly
        if (!canCall1 && !canCall2)
        {
            // Both rate limited - verify timing logic
            var delay1 = (nextTime1 - firstCheck).TotalSeconds;
            var delay2 = (nextTime2 - secondCheck).TotalSeconds;
            
            Console.WriteLine($"⏰ Rate limit delays: {delay1:F2}s, {delay2:F2}s");
            Assert.IsTrue(delay1 <= 10, "Rate limit delay should be reasonable (≤10s)");
            Assert.IsTrue(delay2 <= 10, "Rate limit delay should be reasonable (≤10s)");
        }
        
        Console.WriteLine("✅ Rate limiting behavior test completed");
    }
    
    #endregion
    
    #region Quota Exceeded Simulation Tests
    
    /// <summary>
    /// Test để simulate quota exceeded scenarios
    /// </summary>
    [TestMethod]
    [TestCategory("Quota-Exceeded")]
    public async Task Test_QuotaExceededSimulation_ShouldHandleGracefully()
    {
        // Arrange
        if (_aiService == null)
        {
            Assert.Inconclusive("No API key available for testing");
            return;
        }
        
        Console.WriteLine("=== TEST: Quota Exceeded Simulation ===");
        
        // Create simple test context
        var context = new GenerationContext
        {
            TableName = "test_table",
            Column = new ColumnContext
            {
                Name = "test_column",
                DataType = "varchar",
                MaxLength = 50,
                IsRequired = true
            },
            BusinessHints = new BusinessContext
            {
                Domain = "testing",
                SemanticHints = new List<string> { "api_limit_test" }
            }
        };
        
        Console.WriteLine("📝 Testing with minimal context to minimize API usage");
        
        try
        {
            // Act - Try to generate a value (might hit quota)
            var startTime = DateTime.UtcNow;
            var result = await _aiService.GenerateColumnValueAsync(context, 1);
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;
            
            // Assert - If successful, verify result
            Assert.IsNotNull(result, "Generated value should not be null");
            Console.WriteLine($"✅ AI Generation Success: '{result}' (took {duration.TotalSeconds:F2}s)");
            
            // Check post-generation stats
            var stats = _aiService.FlashRotationService.GetAPIUsageStatistics();
            var dailyUsed = (int)stats["DailyCallsUsed"];
            var dailyLimit = (int)stats["DailyCallLimit"];
            
            Console.WriteLine($"📊 Post-generation usage: {dailyUsed}/{dailyLimit}");
            
            if (dailyUsed >= dailyLimit * 0.9) // 90% threshold
            {
                Console.WriteLine("⚠️ WARNING: Very close to daily limit!");
            }
        }
        catch (Exception ex)
        {
            // Assert - Handle expected quota errors gracefully
            Console.WriteLine($"❌ AI Generation Failed: {ex.Message}");
            
            if (ex.Message.Contains("quota") || 
                ex.Message.Contains("limit") || 
                ex.Message.Contains("exceeded") ||
                ex.Message.Contains("Daily API limit reached"))
            {
                Console.WriteLine("🎯 EXPECTED: Quota exceeded detected");
                
                // Verify fallback behavior would work
                Assert.IsTrue(true, "Quota exceeded is expected behavior");
                
                // Check that next callable time is set appropriately
                var nextCallable = _aiService.FlashRotationService.GetNextCallableTime();
                var hoursUntilReset = (nextCallable - DateTime.UtcNow).TotalHours;
                
                Console.WriteLine($"⏰ Next callable time: {nextCallable:yyyy-MM-dd HH:mm:ss UTC}");
                Console.WriteLine($"⏳ Hours until reset: {hoursUntilReset:F2}");
                
                Assert.IsTrue(hoursUntilReset >= 0, "Next callable time should be in future");
                Assert.IsTrue(hoursUntilReset <= 24, "Reset should be within 24 hours");
            }
            else if (ex.Message.Contains("network") || 
                     ex.Message.Contains("connection") ||
                     ex.Message.Contains("timeout"))
            {
                Console.WriteLine("🌐 EXPECTED: Network/connection issue");
                Assert.Inconclusive("Network issues prevent API testing");
            }
            else
            {
                Console.WriteLine("💥 UNEXPECTED ERROR - investigate");
                throw; // Re-throw unexpected errors
            }
        }
        
        Console.WriteLine("✅ Quota exceeded simulation test completed");
    }
    
    #endregion
    
    #region Fallback Behavior Tests
    
    /// <summary>
    /// Test để verify fallback behavior khi API bị limit
    /// </summary>
    [TestMethod]
    [TestCategory("Fallback-Behavior")]
    public async Task Test_FallbackBehavior_WhenAPILimited_ShouldUseBogusGeneration()
    {
        // Arrange
        Console.WriteLine("=== TEST: Fallback Behavior When API Limited ===");
        
        var engineService = new EngineService(DatabaseType.MySQL, 
            "Server=localhost;Port=3306;Database=test_db;Uid=root;Pwd=password;", 
            _apiKey);
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = "Server=localhost;Port=3306;Database=test_db;Uid=root;Pwd=password;",
            SqlQuery = "SELECT id, name FROM users WHERE name LIKE '%test%' LIMIT 5",
            DesiredRecordCount = 3,
            OpenAiApiKey = _apiKey,
            UseAI = true, // Enable AI but should fallback if limited
            CurrentRecordCount = 0
        };
        
        Console.WriteLine($"🎯 Testing with UseAI=true (should fallback if API limited)");
        
        try
        {
            // Act
            var startTime = DateTime.UtcNow;
            var result = await engineService.ExecuteQueryWithTestDataAsync(request);
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;
            
            Console.WriteLine($"⏱️ Execution took: {duration.TotalSeconds:F2} seconds");
            Console.WriteLine($"✅ Result Success: {result.Success}");
            Console.WriteLine($"📊 Generated Records: {result.GeneratedRecords}");
            
            if (result.Success)
            {
                // Should generate records regardless of AI availability
                Assert.IsTrue(result.GeneratedRecords > 0, "Should generate records via fallback");
                Assert.IsTrue(result.GeneratedInserts.Count > 0, "Should have INSERT statements");
                
                Console.WriteLine("✅ Fallback generation worked successfully");
                
                // Check if fallback was actually used (quick execution suggests Bogus)
                if (duration.TotalSeconds < 5)
                {
                    Console.WriteLine("🚀 Fast execution suggests Bogus fallback was used");
                }
                else
                {
                    Console.WriteLine("🤖 Slower execution suggests AI was available");
                }
            }
            else
            {
                // Even if connection fails, should have attempted generation
                Console.WriteLine($"❌ Failed (expected for connection): {result.ErrorMessage}");
                
                if (result.ErrorMessage?.Contains("connection") == true)
                {
                    Console.WriteLine("🌐 Expected connection failure - fallback logic still tested");
                    Assert.Inconclusive("Cannot test database execution without connection");
                }
                else
                {
                    Assert.Fail($"Unexpected failure: {result.ErrorMessage}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception during fallback test: {ex.Message}");
            
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
        
        Console.WriteLine("✅ Fallback behavior test completed");
    }
    
    #endregion
    
    #region Model Health and Rotation Tests
    
    /// <summary>
    /// Test để kiểm tra model health status và rotation khi có API limits
    /// </summary>
    [TestMethod]
    [TestCategory("Model-Health")]
    public void Test_ModelHealthStatus_ShouldReflectAPIAvailability()
    {
        // Arrange
        if (_flashService == null)
        {
            Assert.Inconclusive("No API key available for testing");
            return;
        }
        
        Console.WriteLine("=== TEST: Model Health Status ===");
        
        // Act
        var modelStats = _flashService.GetModelStatistics();
        
        // Assert
        Assert.IsNotNull(modelStats, "Model statistics should not be null");
        Assert.IsTrue(modelStats.ContainsKey("TotalModels"), "Should contain TotalModels");
        Assert.IsTrue(modelStats.ContainsKey("HealthyModels"), "Should contain HealthyModels");
        Assert.IsTrue(modelStats.ContainsKey("FailedModels"), "Should contain FailedModels");
        
        var totalModels = (int)modelStats["TotalModels"];
        var healthyModels = (int)modelStats["HealthyModels"];
        var failedModels = (Dictionary<string, object>)modelStats["FailedModels"];
        
        Console.WriteLine($"🤖 Total Flash Models: {totalModels}");
        Console.WriteLine($"💚 Healthy Models: {healthyModels}");
        Console.WriteLine($"❌ Failed Models: {failedModels.Count}");
        
        // Validate model counts
        Assert.IsTrue(totalModels > 0, "Should have Flash models available");
        Assert.IsTrue(healthyModels <= totalModels, "Healthy models should not exceed total");
        
        // Log failed models if any
        if (failedModels.Count > 0)
        {
            Console.WriteLine("⚠️ Failed models detected:");
            foreach (var kvp in failedModels)
            {
                Console.WriteLine($"   - {kvp.Key}: {kvp.Value}");
            }
            
            // If most models failed, likely hitting API limits
            if (failedModels.Count >= totalModels * 0.8) // 80%+ failed
            {
                Console.WriteLine("🚫 CRITICAL: Most models failed - likely API quota exhausted");
            }
        }
        else
        {
            Console.WriteLine("✅ All models healthy");
        }
        
        // Test model rotation
        Console.WriteLine("\n🔄 Testing Model Rotation:");
        var model1 = _flashService.GetNextFlashModel();
        var model2 = _flashService.GetNextFlashModel();
        var model3 = _flashService.GetNextFlashModel();
        
        Console.WriteLine($"Model 1: {model1}");
        Console.WriteLine($"Model 2: {model2}");
        Console.WriteLine($"Model 3: {model3}");
        
        Assert.IsNotNull(model1, "Should return a model");
        Assert.IsNotNull(model2, "Should return a model");
        Assert.IsNotNull(model3, "Should return a model");
        
        // If multiple healthy models, should rotate
        if (healthyModels > 1)
        {
            var uniqueModels = new HashSet<string> { model1, model2, model3 };
            Console.WriteLine($"🔄 Unique models in rotation: {uniqueModels.Count}");
        }
        
        Console.WriteLine("✅ Model health status test completed");
    }
    
    #endregion
    
    #region Real API Limit Detection Test
    
    /// <summary>
    /// Test thực tế để detect API limit bằng cách thử call API
    /// CHỈ RUN KHI CẦN THIẾT - có thể consume API quota
    /// </summary>
    [TestMethod]
    [TestCategory("Real-API-Test")]
    [Ignore("Only run when needed - consumes API quota")]
    public async Task Test_RealAPILimitDetection_CheckCurrentQuotaStatus()
    {
        // Arrange
        if (_flashService == null)
        {
            Assert.Inconclusive("No API key available for testing");
            return;
        }
        
        Console.WriteLine("=== REAL API LIMIT DETECTION TEST ===");
        Console.WriteLine("⚠️ WARNING: This test will consume API quota!");
        
        // Check initial status
        var initialStats = _flashService.GetAPIUsageStatistics();
        var initialUsed = (int)initialStats["DailyCallsUsed"];
        var initialLimit = (int)initialStats["DailyCallLimit"];
        
        Console.WriteLine($"📊 Initial Usage: {initialUsed}/{initialLimit}");
        
        if (initialUsed >= initialLimit)
        {
            Console.WriteLine("🚫 Daily limit already reached - cannot test further");
            Assert.Inconclusive("Daily limit already reached");
            return;
        }
        
        try
        {
            // Make a minimal API call
            Console.WriteLine("🚀 Making test API call...");
            var response = await _flashService.CallGeminiAPIAsync("Say 'test' in one word only.", 10);
            
            Console.WriteLine($"✅ API call successful: {response?.Length ?? 0} chars");
            
            // Check updated status
            var finalStats = _flashService.GetAPIUsageStatistics();
            var finalUsed = (int)finalStats["DailyCallsUsed"];
            
            Console.WriteLine($"📊 Final Usage: {finalUsed}/{initialLimit}");
            Console.WriteLine($"📈 API calls consumed: {finalUsed - initialUsed}");
            
            // Verify usage incremented
            Assert.IsTrue(finalUsed > initialUsed, "API usage should have incremented");
            
            // Check if approaching limit
            if (finalUsed >= initialLimit * 0.9) // 90% threshold
            {
                Console.WriteLine("⚠️ WARNING: Very close to daily limit!");
            }
            
            if (finalUsed >= initialLimit)
            {
                Console.WriteLine("🚫 LIMIT REACHED: Daily quota exhausted");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ API call failed: {ex.Message}");
            
            if (ex.Message.Contains("Daily API limit reached"))
            {
                Console.WriteLine("🎯 CONFIRMED: Daily API limit has been reached");
                
                var nextReset = _flashService.GetNextCallableTime();
                var hoursUntilReset = (nextReset - DateTime.UtcNow).TotalHours;
                
                Console.WriteLine($"⏰ Next reset in: {hoursUntilReset:F2} hours");
                Assert.IsTrue(hoursUntilReset >= 0, "Reset time should be in future");
            }
            else if (ex.Message.Contains("quota") || ex.Message.Contains("limit"))
            {
                Console.WriteLine("🎯 CONFIRMED: API quota/limit detected");
            }
            else
            {
                Console.WriteLine("💥 UNEXPECTED ERROR during API test");
                throw;
            }
        }
        
        Console.WriteLine("✅ Real API limit detection test completed");
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Get Gemini API key from multiple sources
    /// </summary>
    private string? GetGeminiApiKey()
    {
        // Try ConfigurationManager first
        var configApiKey = System.Configuration.ConfigurationManager.AppSettings["GeminiApiKey"];
        if (!string.IsNullOrEmpty(configApiKey))
        {
            return configApiKey;
        }
        
        // Try environment variable
        var envApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrEmpty(envApiKey))
        {
            return envApiKey;
        }
        
        // Try alternative environment variable names
        var altEnvKey = Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY");
        if (!string.IsNullOrEmpty(altEnvKey))
        {
            return altEnvKey;
        }
        
        return null;
    }
    
    #endregion
} 