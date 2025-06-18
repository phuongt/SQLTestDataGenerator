using System.Text;
using System.Text.Json;
using SqlTestDataGenerator.Core.Models;
using Serilog;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Enhanced Gemini Flash Rotation Service 
/// Quản lý rotation intelligently qua all Gemini Flash models
/// Tự động failover, recovery, và rate limiting
/// </summary>
public class EnhancedGeminiFlashRotationService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    // Comprehensive Flash Models List từ Google AI API 2025
    private static readonly List<FlashModelInfo> _geminiFlashModels = new List<FlashModelInfo>
    {
        // Gemini 2.5 Flash (Latest Preview) - Best performance
        new FlashModelInfo 
        { 
            ModelName = "gemini-2.5-flash-preview-05-20", 
            Tier = ModelTier.Latest, 
            Description = "2.5 Flash May - Latest với adaptive thinking",
            MaxTokens = 1048576,
            OutputMaxTokens = 65536
        },
        new FlashModelInfo 
        { 
            ModelName = "gemini-2.5-flash-preview-04-17", 
            Tier = ModelTier.Latest, 
            Description = "2.5 Flash April - Hybrid reasoning model",
            MaxTokens = 1048576,
            OutputMaxTokens = 65536
        },
        
        // Gemini 2.0 Flash Series - Next-gen features
        new FlashModelInfo 
        { 
            ModelName = "gemini-2.0-flash", 
            Tier = ModelTier.Stable, 
            Description = "2.0 Flash - Superior speed và capabilities",
            MaxTokens = 1048576,
            OutputMaxTokens = 8192
        },
        new FlashModelInfo 
        { 
            ModelName = "gemini-2.0-flash-001", 
            Tier = ModelTier.Stable, 
            Description = "2.0 Flash Stable - Proven performance",
            MaxTokens = 1048576,
            OutputMaxTokens = 8192
        },
        new FlashModelInfo 
        { 
            ModelName = "gemini-2.0-flash-exp", 
            Tier = ModelTier.Experimental, 
            Description = "2.0 Flash Experimental - Latest features",
            MaxTokens = 1048576,
            OutputMaxTokens = 8192
        },
        new FlashModelInfo 
        { 
            ModelName = "gemini-2.0-flash-lite", 
            Tier = ModelTier.Lite, 
            Description = "2.0 Flash Lite - Cost efficient",
            MaxTokens = 1048576,
            OutputMaxTokens = 8192
        },
        new FlashModelInfo 
        { 
            ModelName = "gemini-2.0-flash-lite-001", 
            Tier = ModelTier.Lite, 
            Description = "2.0 Flash Lite Stable - Budget friendly",
            MaxTokens = 1048576,
            OutputMaxTokens = 8192
        },
        
        // Gemini 1.5 Flash Series - Proven và stable
        new FlashModelInfo 
        { 
            ModelName = "gemini-1.5-flash", 
            Tier = ModelTier.Stable, 
            Description = "1.5 Flash - Fast và versatile",
            MaxTokens = 1048576,
            OutputMaxTokens = 8192
        },
        new FlashModelInfo 
        { 
            ModelName = "gemini-1.5-flash-latest", 
            Tier = ModelTier.Stable, 
            Description = "1.5 Flash Latest - Most recent 1.5",
            MaxTokens = 1048576,
            OutputMaxTokens = 8192
        },
        new FlashModelInfo 
        { 
            ModelName = "gemini-1.5-flash-001", 
            Tier = ModelTier.Stable, 
            Description = "1.5 Flash v001 - Stable release",
            MaxTokens = 1048576,
            OutputMaxTokens = 8192
        },
        new FlashModelInfo 
        { 
            ModelName = "gemini-1.5-flash-002", 
            Tier = ModelTier.Stable, 
            Description = "1.5 Flash v002 - Enhanced version",
            MaxTokens = 1048576,
            OutputMaxTokens = 8192
        },
        new FlashModelInfo 
        { 
            ModelName = "gemini-1.5-flash-8b", 
            Tier = ModelTier.Lite, 
            Description = "1.5 Flash 8B - Lightweight model",
            MaxTokens = 1048576,
            OutputMaxTokens = 8192
        },
        new FlashModelInfo 
        { 
            ModelName = "gemini-1.5-flash-8b-001", 
            Tier = ModelTier.Lite, 
            Description = "1.5 Flash 8B Stable - Small efficient",
            MaxTokens = 1048576,
            OutputMaxTokens = 8192
        }
    };

    // Rotation State Management
    private static int _currentModelIndex = 0;
    private static readonly object _modelRotationLock = new object();
    private static readonly Dictionary<string, ModelHealthInfo> _modelHealth = new Dictionary<string, ModelHealthInfo>();
    
    // Configuration
    private const int MAX_MODEL_FAILURES = 3;
    private const int MODEL_RECOVERY_MINUTES = 10;
    private static readonly SemaphoreSlim _rateLimitSemaphore = new SemaphoreSlim(1, 1);
    private static DateTime _lastApiCall = DateTime.MinValue;
    private static readonly TimeSpan _minDelayBetweenCalls = TimeSpan.FromSeconds(5);

    // Daily API Limit Management
    private static int _dailyCallCount = 0;
    private static DateTime _dailyLimitResetTime = DateTime.UtcNow.Date.AddDays(1); // Reset at midnight next day
    private static readonly int _dailyCallLimit = 100; // Default 100 calls per day - configurable
    private static readonly object _dailyLimitLock = new object();

    public EnhancedGeminiFlashRotationService(string apiKey)
    {
        _logger = Log.Logger.ForContext<EnhancedGeminiFlashRotationService>();
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(3);
        _apiKey = apiKey;
        
        InitializeModelHealth();
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.Warning("Gemini API key is not provided. AI generation will be disabled.");
        }
        else
        {
            _logger.Information("🚀 SUPER OPTIMIZATION generator enabled (1 AI call for all tables)");
            _logger.Information("🔄 OpenAI Service initialized with {ModelCount} Flash models rotation", _geminiFlashModels.Count);
            _logger.Information("🔄 Model rotation includes: {Models}", string.Join(", ", _geminiFlashModels.Select(m => m.ModelName)));
            _logger.Information("⏰ Rate limiting: {DelaySeconds}s between API calls", _minDelayBetweenCalls.TotalSeconds);
            _logger.Information("📊 Daily API limit: {DailyLimit} calls per day, resets at: {ResetTime}",
                _dailyCallLimit, _dailyLimitResetTime.ToString("yyyy-MM-dd HH:mm:ss UTC"));
        }
    }

    /// <summary>
    /// Initialize health info for all models
    /// </summary>
    private void InitializeModelHealth()
    {
        foreach (var model in _geminiFlashModels)
        {
            _modelHealth[model.ModelName] = new ModelHealthInfo
            {
                FailureCount = 0,
                LastFailure = DateTime.MinValue,
                IsHealthy = true
            };
        }
    }

    /// <summary>
    /// Get current active model name for display purposes
    /// </summary>
    public string GetCurrentModelName()
    {
        lock (_modelRotationLock)
        {
            if (_geminiFlashModels.Count == 0) return "No Models Available";
            
            var currentIndex = (_currentModelIndex - 1 + _geminiFlashModels.Count) % _geminiFlashModels.Count;
            var currentModel = _geminiFlashModels[currentIndex];
            
            _logger.Debug("Current active model: {ModelName} (Index: {Index})", 
                currentModel.ModelName, currentIndex);
            
            return currentModel.ModelName;
        }
    }

    /// <summary>
    /// Get current model info with tier and description
    /// </summary>
    public FlashModelInfo GetCurrentModelInfo()
    {
        lock (_modelRotationLock)
        {
            if (_geminiFlashModels.Count == 0) 
                return new FlashModelInfo { ModelName = "No Models", Description = "No models available" };
            
            var currentIndex = (_currentModelIndex - 1 + _geminiFlashModels.Count) % _geminiFlashModels.Count;
            return _geminiFlashModels[currentIndex];
        }
    }

    /// <summary>
    /// Get next best available Flash model với intelligent selection
    /// </summary>
    public string GetNextFlashModel()
    {
        lock (_modelRotationLock)
        {
            // Priority order: Latest > Stable > Lite > Experimental
            var priorityOrder = new[] { ModelTier.Latest, ModelTier.Stable, ModelTier.Lite, ModelTier.Experimental };
            
            foreach (var tier in priorityOrder)
            {
                var healthyModelsInTier = _geminiFlashModels
                    .Where(m => m.Tier == tier && IsModelHealthy(m.ModelName))
                    .ToList();
                
                if (healthyModelsInTier.Any())
                {
                    // Round-robin within the tier
                    var selectedModel = healthyModelsInTier[_currentModelIndex % healthyModelsInTier.Count];
                    _currentModelIndex++;
                    
                    _logger.Information("🔄 Selected Flash model: {Model} (tier: {Tier}, index: {Index})", 
                        selectedModel.ModelName, selectedModel.Tier, _currentModelIndex);
                    
                    return selectedModel.ModelName;
                }
            }
            
            // Emergency fallback: reset all failures và use first model
            _logger.Warning("⚠️ All Flash models appear unhealthy, resetting failure counts");
            ResetAllModelFailures();
            var fallbackModel = _geminiFlashModels.First().ModelName;
            _logger.Information("🔄 Using fallback model: {Model}", fallbackModel);
            return fallbackModel;
        }
    }

    /// <summary>
    /// Check if model is healthy (not failed too many times recently)
    /// </summary>
    private bool IsModelHealthy(string modelName)
    {
        if (!_modelHealth.ContainsKey(modelName)) return true;
        
        var health = _modelHealth[modelName];
        if (health.FailureCount < MAX_MODEL_FAILURES) return true;
        
        // Check if enough time has passed for recovery
        var timeSinceLastFailure = DateTime.UtcNow - health.LastFailure;
        if (timeSinceLastFailure.TotalMinutes > MODEL_RECOVERY_MINUTES)
        {
            // Reset failure count for recovery
            health.FailureCount = 0;
            health.IsHealthy = true;
            _logger.Information("🔄 Model {Model} recovered after {Minutes} minutes", modelName, MODEL_RECOVERY_MINUTES);
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Mark model as failed và increment failure count
    /// </summary>
    public void MarkModelAsFailed(string modelName, Exception ex)
    {
        lock (_modelRotationLock)
        {
            if (!_modelHealth.ContainsKey(modelName))
            {
                _modelHealth[modelName] = new ModelHealthInfo();
            }
            
            var health = _modelHealth[modelName];
            health.FailureCount++;
            health.LastFailure = DateTime.UtcNow;
            health.IsHealthy = health.FailureCount < MAX_MODEL_FAILURES;
            
            _logger.Warning("❌ Model {Model} failed (count: {Count}): {Error}", 
                modelName, health.FailureCount, ex.Message);
        }
    }

    /// <summary>
    /// Reset all model failures để recovery
    /// </summary>
    private void ResetAllModelFailures()
    {
        foreach (var health in _modelHealth.Values)
        {
            health.FailureCount = 0;
            health.LastFailure = DateTime.MinValue;
            health.IsHealthy = true;
        }
    }

    /// <summary>
    /// Enhanced API call với rotation và smart retry
    /// </summary>
    public async Task<string> CallGeminiAPIAsync(string prompt, int maxTokens = 4000)
    {
        // CRITICAL: Check time availability trước khi proceed
        if (!CanCallAPINow())
        {
            var nextCallableTime = GetNextCallableTime();
            var waitTime = nextCallableTime - DateTime.UtcNow;
            
            if (waitTime.TotalHours > 1) // If need to wait more than 1 hour, likely daily limit
            {
                _logger.Error("🚫 API call blocked - Daily limit reached. Next callable time: {NextTime} (wait: {WaitHours} hours)",
                    nextCallableTime.ToString("yyyy-MM-dd HH:mm:ss UTC"), Math.Ceiling(waitTime.TotalHours));
                throw new InvalidOperationException($"Daily API limit reached. Next callable time: {nextCallableTime:yyyy-MM-dd HH:mm:ss UTC}");
            }
            else
            {
                _logger.Information("⏰ API call delayed - Rate limit. Waiting {WaitSeconds} seconds until {NextTime}",
                    Math.Ceiling(waitTime.TotalSeconds), nextCallableTime.ToString("HH:mm:ss UTC"));
                await Task.Delay(waitTime);
            }
        }

        // Rate limiting semaphore: chỉ cho phép 1 call concurrent
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            // Double-check time availability after acquiring semaphore
            var timeSinceLastCall = DateTime.UtcNow - _lastApiCall;
            if (timeSinceLastCall < _minDelayBetweenCalls)
            {
                var delayNeeded = _minDelayBetweenCalls - timeSinceLastCall;
                _logger.Information("⏰ Final rate limit check: waiting {DelayMs}ms before API call", 
                    delayNeeded.TotalMilliseconds);
                await Task.Delay(delayNeeded);
            }

            // Increment daily call count
            lock (_dailyLimitLock)
            {
                _dailyCallCount++;
                _logger.Information("📊 Daily API usage: {CurrentCount}/{DailyLimit} calls used", 
                    _dailyCallCount, _dailyCallLimit);
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = maxTokens,
                    topP = 0.8,
                    topK = 40
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Smart retry logic với model rotation (max 3 tries hoặc số model available)
            Exception lastException = null;
            int maxRetries = Math.Min(3, _geminiFlashModels.Count(m => IsModelHealthy(m.ModelName)));
            if (maxRetries == 0) maxRetries = 1; // At least try once
            
            for (int retry = 0; retry < maxRetries; retry++)
            {
                var currentModel = GetNextFlashModel();
                var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{currentModel}:generateContent?key={_apiKey}";
                
                try
                {
                    _logger.Information("🚀 Making Gemini API call với model: {Model} (attempt {Attempt}/{MaxRetries})", 
                        currentModel, retry + 1, maxRetries);
                    
                    var response = await _httpClient.PostAsync(requestUrl, content);
                    _lastApiCall = DateTime.UtcNow;

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        _logger.Information("✅ Gemini API call successful với model: {Model}", currentModel);
                        return responseBody;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        var exception = new Exception($"Gemini API call failed: {response.StatusCode} - {errorContent}");
                        
                        _logger.Warning("⚠️ Model {Model} returned error: {StatusCode} - {Error}", 
                            currentModel, response.StatusCode, errorContent);
                        
                        // Mark model as failed if it's a rate limit or server error
                        if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                        {
                            MarkModelAsFailed(currentModel, exception);
                        }
                        
                        lastException = exception;
                        
                        // If this was the last retry, throw
                        if (retry == maxRetries - 1)
                        {
                            throw exception;
                        }
                        
                        // Progressive delay between attempts
                        await Task.Delay(1000 * (retry + 1));
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.Warning("🌐 Network error với model {Model}: {Error}", currentModel, httpEx.Message);
                    MarkModelAsFailed(currentModel, httpEx);
                    lastException = httpEx;
                    
                    if (retry == maxRetries - 1)
                    {
                        break; // Exit để throw consolidated error
                    }
                    
                    await Task.Delay(2000 * (retry + 1));
                }
                catch (TaskCanceledException timeoutEx)
                {
                    _logger.Warning("⏱️ Timeout với model {Model}: {Error}", currentModel, timeoutEx.Message);
                    MarkModelAsFailed(currentModel, timeoutEx);
                    lastException = timeoutEx;
                    
                    if (retry == maxRetries - 1)
                    {
                        break; // Exit để throw consolidated error
                    }
                    
                    await Task.Delay(1500 * (retry + 1));
                }
            }
            
            // If all retries failed, report "limit API AI" error as requested
            _logger.Error("❌ All Flash models failed after {Retries} attempts. Limit API AI reached.", maxRetries);
            throw lastException ?? new Exception("Limit API AI: All Gemini Flash models exhausted after multiple retry attempts");
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    /// <summary>
    /// Get current model statistics
    /// </summary>
    public Dictionary<string, object> GetModelStatistics()
    {
        lock (_modelRotationLock)
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalModels"] = _geminiFlashModels.Count,
                ["HealthyModels"] = _modelHealth.Count(h => h.Value.IsHealthy),
                ["CurrentIndex"] = _currentModelIndex,
                ["ModelsByTier"] = _geminiFlashModels.GroupBy(m => m.Tier).ToDictionary(
                    g => g.Key.ToString(), 
                    g => g.Count()
                ),
                ["FailedModels"] = _modelHealth.Where(h => !h.Value.IsHealthy)
                    .ToDictionary(
                        kvp => kvp.Key, 
                        kvp => new { kvp.Value.FailureCount, kvp.Value.LastFailure }
                    )
            };
            
            return stats;
        }
    }

    /// <summary>
    /// Check if API call is available right now (both rate limit và daily limit)
    /// </summary>
    public bool CanCallAPINow()
    {
        lock (_dailyLimitLock)
        {
            // Check if daily limit needs reset
            if (DateTime.UtcNow >= _dailyLimitResetTime)
            {
                ResetDailyLimits();
            }

            // Check daily quota
            if (_dailyCallCount >= _dailyCallLimit)
            {
                _logger.Warning("🚫 Daily API limit reached: {CurrentCount}/{DailyLimit}. Next reset: {ResetTime}",
                    _dailyCallCount, _dailyCallLimit, _dailyLimitResetTime.ToString("yyyy-MM-dd HH:mm:ss UTC"));
                return false;
            }

            // Check rate limiting (5s between calls)
            var timeSinceLastCall = DateTime.UtcNow - _lastApiCall;
            if (timeSinceLastCall < _minDelayBetweenCalls)
            {
                _logger.Information("⏰ Rate limit: need to wait {DelayMs}ms before next call",
                    (_minDelayBetweenCalls - timeSinceLastCall).TotalMilliseconds);
                return false;
            }

            // Check if any healthy models available
            var healthyModelsCount = _geminiFlashModels.Count(m => IsModelHealthy(m.ModelName));
            if (healthyModelsCount == 0)
            {
                _logger.Warning("🔴 No healthy models available for API calls");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Get the next time when API call will be available
    /// </summary>
    public DateTime GetNextCallableTime()
    {
        lock (_dailyLimitLock)
        {
            // Check if daily limit needs reset
            if (DateTime.UtcNow >= _dailyLimitResetTime)
            {
                ResetDailyLimits();
            }

            // If daily limit reached, return reset time (next day)
            if (_dailyCallCount >= _dailyCallLimit)
            {
                return _dailyLimitResetTime;
            }

            // Otherwise, return time based on rate limiting
            var nextRateAllowedTime = _lastApiCall + _minDelayBetweenCalls;
            return nextRateAllowedTime > DateTime.UtcNow ? nextRateAllowedTime : DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Reset daily limits (called at midnight or when time passed)
    /// </summary>
    private void ResetDailyLimits()
    {
        var oldCount = _dailyCallCount;
        _dailyCallCount = 0;
        _dailyLimitResetTime = DateTime.UtcNow.Date.AddDays(1); // Next midnight
        
        _logger.Information("🔄 Daily API limits reset. Previous count: {OldCount}, Next reset: {NextReset}",
            oldCount, _dailyLimitResetTime.ToString("yyyy-MM-dd HH:mm:ss UTC"));
    }

    /// <summary>
    /// Get current API usage statistics
    /// </summary>
    public Dictionary<string, object> GetAPIUsageStatistics()
    {
        lock (_dailyLimitLock)
        {
            // Check if daily limit needs reset
            if (DateTime.UtcNow >= _dailyLimitResetTime)
            {
                ResetDailyLimits();
            }

            return new Dictionary<string, object>
            {
                ["DailyCallsUsed"] = _dailyCallCount,
                ["DailyCallLimit"] = _dailyCallLimit,
                ["DailyCallsRemaining"] = Math.Max(0, _dailyCallLimit - _dailyCallCount),
                ["DailyResetTime"] = _dailyLimitResetTime.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ["LastAPICall"] = _lastApiCall.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ["NextCallableTime"] = GetNextCallableTime().ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ["CanCallNow"] = CanCallAPINow(),
                ["MinDelayBetweenCalls"] = _minDelayBetweenCalls.TotalSeconds + " seconds"
            };
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

#region Supporting Models và Enums

/// <summary>
/// Flash Model Information
/// </summary>
public class FlashModelInfo
{
    public string ModelName { get; set; } = string.Empty;
    public ModelTier Tier { get; set; }
    public string Description { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public int OutputMaxTokens { get; set; }
}

/// <summary>
/// Model tier classification
/// </summary>
public enum ModelTier
{
    Latest = 1,      // Newest features, best performance
    Stable = 2,      // Proven và reliable
    Lite = 3,        // Cost efficient, smaller
    Experimental = 4 // Beta features, may be unstable
}

/// <summary>
/// Model health tracking information
/// </summary>
public class ModelHealthInfo
{
    public int FailureCount { get; set; }
    public DateTime LastFailure { get; set; }
    public bool IsHealthy { get; set; } = true;
}

#endregion 