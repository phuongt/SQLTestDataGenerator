using SqlTestDataGenerator.Core.Services;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Mock service để simulate các API limit scenarios
/// Giúp test behavior mà không cần consume real API quota
/// </summary>
public class MockAPILimitService
{
    public APILimitScenario CurrentScenario { get; set; } = APILimitScenario.Normal;
    public int SimulatedDailyUsed { get; set; } = 0;
    public int SimulatedDailyLimit { get; set; } = 100;
    public DateTime SimulatedLastCall { get; set; } = DateTime.MinValue;
    public TimeSpan SimulatedRateLimit { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Simulate API availability checking
    /// </summary>
    public bool CanCallAPINow()
    {
        switch (CurrentScenario)
        {
            case APILimitScenario.DailyLimitReached:
                return false;
                
            case APILimitScenario.RateLimited:
                var timeSinceLastCall = DateTime.UtcNow - SimulatedLastCall;
                return timeSinceLastCall >= SimulatedRateLimit;
                
            case APILimitScenario.QuotaExceeded:
                return false;
                
            case APILimitScenario.NetworkError:
                return false;
                
            case APILimitScenario.Normal:
            default:
                return SimulatedDailyUsed < SimulatedDailyLimit;
        }
    }
    
    /// <summary>
    /// Simulate getting next callable time
    /// </summary>
    public DateTime GetNextCallableTime()
    {
        switch (CurrentScenario)
        {
            case APILimitScenario.DailyLimitReached:
                // Next day reset
                return DateTime.UtcNow.Date.AddDays(1);
                
            case APILimitScenario.RateLimited:
                return SimulatedLastCall + SimulatedRateLimit;
                
            case APILimitScenario.QuotaExceeded:
                // Next day reset
                return DateTime.UtcNow.Date.AddDays(1);
                
            case APILimitScenario.NetworkError:
                // Retry after short delay
                return DateTime.UtcNow.AddMinutes(5);
                
            case APILimitScenario.Normal:
            default:
                return DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Simulate API usage statistics
    /// </summary>
    public Dictionary<string, object> GetAPIUsageStatistics()
    {
        return new Dictionary<string, object>
        {
            ["DailyCallsUsed"] = SimulatedDailyUsed,
            ["DailyCallLimit"] = SimulatedDailyLimit,
            ["DailyCallsRemaining"] = Math.Max(0, SimulatedDailyLimit - SimulatedDailyUsed),
            ["DailyResetTime"] = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss UTC"),
            ["LastAPICall"] = SimulatedLastCall.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            ["NextCallableTime"] = GetNextCallableTime().ToString("yyyy-MM-dd HH:mm:ss UTC"),
            ["CanCallNow"] = CanCallAPINow(),
            ["MinDelayBetweenCalls"] = $"{SimulatedRateLimit.TotalSeconds} seconds",
            ["CurrentScenario"] = CurrentScenario.ToString()
        };
    }
    
    /// <summary>
    /// Simulate API call attempt
    /// </summary>
    public async Task<string> SimulateAPICallAsync(string prompt)
    {
        // Update last call time
        SimulatedLastCall = DateTime.UtcNow;
        
        // Check if call should succeed based on scenario
        if (!CanCallAPINow())
        {
            throw CreateScenarioException();
        }
        
        // Increment usage counter
        SimulatedDailyUsed++;
        
        // Simulate processing delay
        await Task.Delay(100);
        
        // Return mock response
        return CreateMockResponse(prompt);
    }
    
    /// <summary>
    /// Create exception based on current scenario
    /// </summary>
    private Exception CreateScenarioException()
    {
        return CurrentScenario switch
        {
            APILimitScenario.DailyLimitReached => 
                new InvalidOperationException("Daily API limit reached. Next callable time: tomorrow"),
                
            APILimitScenario.RateLimited => 
                new InvalidOperationException($"Rate limited. Next callable time: {GetNextCallableTime():yyyy-MM-dd HH:mm:ss UTC}"),
                
            APILimitScenario.QuotaExceeded => 
                new Exception("Quota exceeded. Please try again tomorrow."),
                
            APILimitScenario.NetworkError => 
                new HttpRequestException("Network error - unable to reach API"),
                
            _ => new Exception("Unknown API error")
        };
    }
    
    /// <summary>
    /// Create mock API response
    /// </summary>
    private string CreateMockResponse(string prompt)
    {
        // Simple mock response based on prompt
        var mockResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = GenerateMockValue(prompt) }
                        }
                    }
                }
            }
        };
        
        return System.Text.Json.JsonSerializer.Serialize(mockResponse);
    }
    
    /// <summary>
    /// Generate mock value based on prompt
    /// </summary>
    private string GenerateMockValue(string prompt)
    {
        if (prompt.Contains("company"))
            return "Mock Company Inc";
        if (prompt.Contains("name"))
            return "Mock Name";
        if (prompt.Contains("email"))
            return "mock@example.com";
        if (prompt.Contains("username"))
            return "mock_user";
        if (prompt.Contains("test"))
            return "test";
            
        return "mock_value";
    }
    
    /// <summary>
    /// Reset simulation state
    /// </summary>
    public void Reset()
    {
        CurrentScenario = APILimitScenario.Normal;
        SimulatedDailyUsed = 0;
        SimulatedDailyLimit = 100;
        SimulatedLastCall = DateTime.MinValue;
        SimulatedRateLimit = TimeSpan.FromSeconds(5);
    }
    
    /// <summary>
    /// Set scenario to simulate daily limit reached
    /// </summary>
    public void SimulateDailyLimitReached()
    {
        CurrentScenario = APILimitScenario.DailyLimitReached;
        SimulatedDailyUsed = SimulatedDailyLimit;
    }
    
    /// <summary>
    /// Set scenario to simulate rate limiting
    /// </summary>
    public void SimulateRateLimit()
    {
        CurrentScenario = APILimitScenario.RateLimited;
        SimulatedLastCall = DateTime.UtcNow; // Just made a call
    }
    
    /// <summary>
    /// Set scenario to simulate quota exceeded
    /// </summary>
    public void SimulateQuotaExceeded()
    {
        CurrentScenario = APILimitScenario.QuotaExceeded;
        SimulatedDailyUsed = SimulatedDailyLimit + 10; // Over limit
    }
    
    /// <summary>
    /// Set scenario to simulate network error
    /// </summary>
    public void SimulateNetworkError()
    {
        CurrentScenario = APILimitScenario.NetworkError;
    }
    
    /// <summary>
    /// Simulate approaching daily limit (80%+ usage)
    /// </summary>
    public void SimulateApproachingLimit()
    {
        CurrentScenario = APILimitScenario.Normal;
        SimulatedDailyUsed = (int)(SimulatedDailyLimit * 0.85); // 85% used
    }
}

/// <summary>
/// Different API limit scenarios for testing
/// </summary>
public enum APILimitScenario
{
    /// <summary>
    /// Normal operation - API available
    /// </summary>
    Normal,
    
    /// <summary>
    /// Daily limit reached - cannot call until tomorrow
    /// </summary>
    DailyLimitReached,
    
    /// <summary>
    /// Rate limited - must wait between calls
    /// </summary>
    RateLimited,
    
    /// <summary>
    /// Quota exceeded - similar to daily limit
    /// </summary>
    QuotaExceeded,
    
    /// <summary>
    /// Network error - cannot reach API
    /// </summary>
    NetworkError
}

/// <summary>
/// Test cases using MockAPILimitService
/// </summary>
[TestClass]
[TestCategory("Mock-API-Limits")]
public class MockAPILimitTests
{
    private MockAPILimitService _mockService = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _mockService = new MockAPILimitService();
    }
    
    [TestMethod]
    public void Test_NormalScenario_ShouldAllowAPICalls()
    {
        // Arrange
        _mockService.CurrentScenario = APILimitScenario.Normal;
        
        // Act & Assert
        Assert.IsTrue(_mockService.CanCallAPINow(), "Should allow calls in normal scenario");
        
        var stats = _mockService.GetAPIUsageStatistics();
        Assert.AreEqual(0, stats["DailyCallsUsed"]);
        Assert.AreEqual(100, stats["DailyCallsRemaining"]);
        Assert.AreEqual(true, stats["CanCallNow"]);
        
        Console.WriteLine("✅ Normal scenario test passed");
    }
    
    [TestMethod]
    public async Task Test_DailyLimitReached_ShouldBlockCalls()
    {
        // Arrange
        _mockService.SimulateDailyLimitReached();
        
        // Act & Assert
        Assert.IsFalse(_mockService.CanCallAPINow(), "Should block calls when daily limit reached");
        
        var stats = _mockService.GetAPIUsageStatistics();
        Assert.AreEqual(100, stats["DailyCallsUsed"]);
        Assert.AreEqual(0, stats["DailyCallsRemaining"]);
        Assert.AreEqual(false, stats["CanCallNow"]);
        
        // Should throw exception when trying to call
        try
        {
            await _mockService.SimulateAPICallAsync("test");
            Assert.Fail("Should have thrown exception for daily limit");
        }
        catch (InvalidOperationException ex)
        {
            Assert.IsTrue(ex.Message.Contains("Daily API limit reached"));
            Console.WriteLine($"✅ Expected exception: {ex.Message}");
        }
        
        Console.WriteLine("✅ Daily limit reached test passed");
    }
    
    [TestMethod]
    public void Test_RateLimiting_ShouldEnforceDelay()
    {
        // Arrange
        _mockService.SimulateRateLimit();
        
        // Act & Assert
        Assert.IsFalse(_mockService.CanCallAPINow(), "Should be rate limited immediately after call");
        
        var nextCallTime = _mockService.GetNextCallableTime();
        var delaySeconds = (nextCallTime - DateTime.UtcNow).TotalSeconds;
        
        Assert.IsTrue(delaySeconds > 0, "Should have delay before next call");
        Assert.IsTrue(delaySeconds <= 10, "Delay should be reasonable (≤10 seconds)");
        
        Console.WriteLine($"✅ Rate limiting test passed - delay: {delaySeconds:F2}s");
    }
    
    [TestMethod]
    public async Task Test_QuotaExceeded_ShouldHandleGracefully()
    {
        // Arrange
        _mockService.SimulateQuotaExceeded();
        
        // Act & Assert
        Assert.IsFalse(_mockService.CanCallAPINow(), "Should block calls when quota exceeded");
        
        try
        {
            await _mockService.SimulateAPICallAsync("test");
            Assert.Fail("Should have thrown exception for quota exceeded");
        }
        catch (Exception ex)
        {
            Assert.IsTrue(ex.Message.Contains("Quota exceeded"));
            Console.WriteLine($"✅ Expected quota exception: {ex.Message}");
        }
        
        Console.WriteLine("✅ Quota exceeded test passed");
    }
    
    [TestMethod]
    public async Task Test_NetworkError_ShouldReturnAppropriateError()
    {
        // Arrange
        _mockService.SimulateNetworkError();
        
        // Act & Assert
        try
        {
            await _mockService.SimulateAPICallAsync("test");
            Assert.Fail("Should have thrown network exception");
        }
        catch (HttpRequestException ex)
        {
            Assert.IsTrue(ex.Message.Contains("Network error"));
            Console.WriteLine($"✅ Expected network exception: {ex.Message}");
        }
        
        Console.WriteLine("✅ Network error test passed");
    }
    
    [TestMethod]
    public void Test_ApproachingLimit_ShouldWarnUser()
    {
        // Arrange
        _mockService.SimulateApproachingLimit();
        
        // Act
        var stats = _mockService.GetAPIUsageStatistics();
        var usedCalls = (int)stats["DailyCallsUsed"];
        var totalCalls = (int)stats["DailyCallLimit"];
        var usagePercentage = (double)usedCalls / totalCalls * 100;
        
        // Assert
        Assert.IsTrue(usagePercentage >= 80, "Should be approaching limit (≥80%)");
        Assert.IsTrue(_mockService.CanCallAPINow(), "Should still allow calls");
        
        Console.WriteLine($"📊 Usage: {usedCalls}/{totalCalls} ({usagePercentage:F1}%)");
        
        if (usagePercentage >= 90)
        {
            Console.WriteLine("⚠️ WARNING: Very close to daily limit!");
        }
        else if (usagePercentage >= 80)
        {
            Console.WriteLine("⚠️ WARNING: Approaching daily limit");
        }
        
        Console.WriteLine("✅ Approaching limit test passed");
    }
    
    [TestMethod]
    public async Task Test_SuccessfulAPICall_ShouldIncrementUsage()
    {
        // Arrange
        _mockService.Reset();
        
        // Act
        var initialStats = _mockService.GetAPIUsageStatistics();
        var initialUsed = (int)initialStats["DailyCallsUsed"];
        
        var response = await _mockService.SimulateAPICallAsync("test company name");
        
        var finalStats = _mockService.GetAPIUsageStatistics();
        var finalUsed = (int)finalStats["DailyCallsUsed"];
        
        // Assert
        Assert.IsNotNull(response, "Should return response");
        Assert.IsTrue(response.Contains("Mock"), "Should contain mock data");
        Assert.AreEqual(initialUsed + 1, finalUsed, "Should increment usage counter");
        
        Console.WriteLine($"📊 Usage incremented: {initialUsed} → {finalUsed}");
        Console.WriteLine($"📝 Mock response: {response}");
        Console.WriteLine("✅ Successful API call test passed");
    }
} 