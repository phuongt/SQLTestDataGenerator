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
    // Model rotation tracking - track last used model để tránh dùng lại ngay lập tức
    private static readonly Dictionary<string, DateTime> _lastModelUsage = new Dictionary<string, DateTime>();
    private static readonly TimeSpan _modelCooldownPeriod = TimeSpan.Zero; // No cooldown - active rotation

    // API Limit Management - Hourly and Daily
    private static int _dailyCallCount = 0;
    private static int _hourlyCallCount = 0;
    private static DateTime _dailyLimitResetTime = DateTime.UtcNow.Date.AddDays(1); // Reset at midnight next day
    private static DateTime _hourlyLimitResetTime = DateTime.UtcNow.AddHours(1); // Reset every hour
    private static readonly int _dailyCallLimit = 100; // Default 100 calls per day - configurable
    private static readonly int _hourlyCallLimit = 15; // Default 15 calls per hour - configurable
    private static readonly object _limitLock = new object();

    // Model Health Persistence
    private static readonly string _modelHealthFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "model-health.json");
    private static readonly object _persistenceLock = new object();
    private static DateTime _lastHealthSave = DateTime.MinValue;
    private static readonly TimeSpan _healthSaveInterval = TimeSpan.FromMinutes(1); // Save every minute

    // Current prompt tracking for UI display
    private string _currentPrompt = string.Empty;
    private DateTime _currentPromptTimestamp = DateTime.MinValue;
    private string _currentModelUsed = string.Empty;

    public EnhancedGeminiFlashRotationService(string apiKey)
    {
        _logger = Log.Logger.ForContext<EnhancedGeminiFlashRotationService>();
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(3);
        _apiKey = apiKey;
        
        LoadModelHealthFromFile(); // Load persisted health data
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
            _logger.Information("🔄 Model rotation: {CooldownSeconds}s cooldown per model", _modelCooldownPeriod.TotalSeconds);
            _logger.Information("📊 API limits: {HourlyLimit}/hour (resets: {HourlyReset}), {DailyLimit}/day (resets: {DailyReset})",
                _hourlyCallLimit, _hourlyLimitResetTime.ToString("HH:mm:ss UTC"), 
                _dailyCallLimit, _dailyLimitResetTime.ToString("yyyy-MM-dd HH:mm:ss UTC"));
            _logger.Information("💾 Model health persistence: {HealthFile}", _modelHealthFile);
        }
    }

    /// <summary>
    /// Initialize health info for all models
    /// </summary>
    private void InitializeModelHealth()
    {
        foreach (var model in _geminiFlashModels)
        {
            // Only initialize if not already loaded from file
            if (!_modelHealth.ContainsKey(model.ModelName))
            {
                _modelHealth[model.ModelName] = new ModelHealthInfo
            {
                FailureCount = 0,
                LastFailure = DateTime.MinValue,
                    IsHealthy = true,
                    LastFailureReason = null,
                    RecoveryTime = null,
                    LimitType = null
                };
            }
        }
    }

    /// <summary>
    /// Load model health data from persistent file
    /// </summary>
    private void LoadModelHealthFromFile()
    {
        try
        {
            if (!File.Exists(_modelHealthFile))
            {
                _logger.Information("📁 No existing model health file found. Starting fresh.");
                return;
            }

            var jsonContent = File.ReadAllText(_modelHealthFile);
            var healthData = JsonSerializer.Deserialize<Dictionary<string, ModelHealthInfo>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (healthData != null)
            {
                lock (_modelRotationLock)
                {
                    foreach (var kvp in healthData)
                    {
                        var modelName = kvp.Key;
                        var healthInfo = kvp.Value;

                        // Validate recovery time - if it's in the past, reset the model
                        if (healthInfo.RecoveryTime.HasValue && healthInfo.RecoveryTime.Value <= DateTime.UtcNow)
                        {
                            _logger.Information("🔄 Model {Model} recovery time expired, resetting health", modelName);
                            healthInfo.FailureCount = 0;
                            healthInfo.IsHealthy = true;
                            healthInfo.LastFailureReason = null;
                            healthInfo.RecoveryTime = null;
                            healthInfo.LimitType = null;
                        }

                        _modelHealth[modelName] = healthInfo;
                    }
                }

                var loadedCount = healthData.Count;
                var healthyCount = healthData.Values.Count(h => h.IsHealthy);
                _logger.Information("📂 Loaded model health data: {LoadedCount} models, {HealthyCount} healthy", loadedCount, healthyCount);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning("⚠️ Failed to load model health data: {Error}. Starting fresh.", ex.Message);
            // Clear any partial data
            lock (_modelRotationLock)
            {
                _modelHealth.Clear();
            }
        }
    }

    /// <summary>
    /// Save model health data to persistent file
    /// </summary>
    private void SaveModelHealthToFile()
    {
        try
        {
            // Ensure data directory exists
            var dataDir = Path.GetDirectoryName(_modelHealthFile);
            if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            Dictionary<string, ModelHealthInfo> healthDataToSave;
            lock (_modelRotationLock)
            {
                healthDataToSave = new Dictionary<string, ModelHealthInfo>(_modelHealth);
            }

            var jsonContent = JsonSerializer.Serialize(healthDataToSave, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Use atomic write to prevent corruption
            var tempFile = _modelHealthFile + ".tmp";
            File.WriteAllText(tempFile, jsonContent);
            File.Move(tempFile, _modelHealthFile, true);

            _lastHealthSave = DateTime.UtcNow;
            _logger.Debug("💾 Model health data saved: {ModelCount} models", healthDataToSave.Count);
        }
        catch (Exception ex)
        {
            _logger.Warning("⚠️ Failed to save model health data: {Error}", ex.Message);
        }
    }

    /// <summary>
    /// Trigger health data save if enough time has passed
    /// </summary>
    private void TrySaveModelHealth()
    {
        if (DateTime.UtcNow - _lastHealthSave >= _healthSaveInterval)
        {
            SaveModelHealthToFile();
        }
    }

    /// <summary>
    /// Force save model health data immediately
    /// </summary>
    public void ForceSaveModelHealth()
    {
        SaveModelHealthToFile();
    }

    /// <summary>
    /// Force reload model health data from file
    /// </summary>
    public void ForceReloadModelHealth()
    {
        LoadModelHealthFromFile();
    }

    /// <summary>
    /// Get the path to the model health file
    /// </summary>
    public string GetModelHealthFilePath()
    {
        return _modelHealthFile;
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
            
            // Removed debug logging to prevent log spam from UI timer
            // _logger.Debug("Current active model: {ModelName} (Index: {Index})", 
            //     currentModel.ModelName, currentIndex);
            
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
                    // Active rotation: always move to next model, no cooldown
                    var selectedModel = healthyModelsInTier[_currentModelIndex % healthyModelsInTier.Count];
                    _currentModelIndex++;
                    
                    _logger.Information("🔄 Active rotation - Selected Flash model: {Model} (tier: {Tier}, index: {Index})", 
                        selectedModel.ModelName, selectedModel.Tier, _currentModelIndex);
                    
                    return selectedModel.ModelName;
                }
                else
                {
                    // Log which models are being skipped in this tier
                    var unhealthyModelsInTier = _geminiFlashModels
                        .Where(m => m.Tier == tier && !IsModelHealthy(m.ModelName))
                        .ToList();
                    
                    if (unhealthyModelsInTier.Any())
                    {
                        var skippedModels = string.Join(", ", unhealthyModelsInTier.Select(m => 
                            $"{m.ModelName}({_modelHealth.GetValueOrDefault(m.ModelName)?.LastFailureReason ?? "unknown"})"));
                        _logger.Debug("⏭️ Skipping {Tier} tier models: {Models}", tier, skippedModels);
                    }
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
    /// Check if model is healthy based on calculated recovery time from API limits
    /// </summary>
    private bool IsModelHealthy(string modelName)
    {
        if (!_modelHealth.ContainsKey(modelName)) return true;
        
        var health = _modelHealth[modelName];
        
        // If model has failed too many times, check recovery time
        if (health.FailureCount >= MAX_MODEL_FAILURES)
        {
            // Check if model is permanently disabled (404)
            if (health.LastFailureReason?.Contains("404_MODEL_NOT_FOUND") == true)
            {
                _logger.Debug("🚫 Model {Model} permanently disabled (404)", modelName);
                return false;
            }
            
            // Use calculated recovery time if available, otherwise fallback to default
            if (health.RecoveryTime.HasValue)
            {
                // Check if model is permanently disabled (recovery time = MaxValue)
                if (health.RecoveryTime.Value == DateTime.MaxValue)
                {
                    _logger.Debug("🚫 Model {Model} permanently disabled", modelName);
                    return false;
                }
                
                if (DateTime.UtcNow >= health.RecoveryTime.Value)
                {
                    // Model has recovered - reset health
                    health.FailureCount = 0;
                    health.IsHealthy = true;
                    health.LastFailureReason = null;
                    health.RecoveryTime = null;
                    health.LimitType = null;
                    
                    var recoveryDuration = health.RecoveryTime.Value - health.LastFailure;
                    _logger.Information("🔄 Model {Model} recovered after {Duration} (limit type: {LimitType})", 
                        modelName, recoveryDuration.ToString(@"hh\:mm\:ss"), health.LimitType ?? "unknown");
                    
                    // Trigger save to persist the recovery
                    TrySaveModelHealth();
                    return true;
                }
                else
                {
                    // Model is still in recovery period
                    var timeRemaining = health.RecoveryTime.Value - DateTime.UtcNow;
                    _logger.Debug("⏭️ Skipping model {Model} - recovery in {TimeRemaining} (limit: {LimitType})", 
                        modelName, timeRemaining.ToString(@"mm\:ss"), health.LimitType ?? "unknown");
                    return false;
                }
            }
            else
            {
                // Fallback to old logic for backward compatibility
                var timeSinceLastFailure = DateTime.UtcNow - health.LastFailure;
                var recoveryTimeMinutes = health.LastFailureReason?.Contains("429") == true ? 
                    MODEL_RECOVERY_MINUTES * 2 : MODEL_RECOVERY_MINUTES;
                
                if (timeSinceLastFailure.TotalMinutes > recoveryTimeMinutes)
        {
            // Reset failure count for recovery
            health.FailureCount = 0;
            health.IsHealthy = true;
                    health.LastFailureReason = null;
                    _logger.Information("🔄 Model {Model} recovered after {Minutes} minutes (fallback logic)", 
                        modelName, recoveryTimeMinutes);
            return true;
                }
                
                // Model is still unhealthy - completely skip it
                _logger.Debug("⏭️ Skipping unhealthy model {Model} (failures: {Count}, last failure: {LastFailure}, reason: {Reason})", 
                    modelName, health.FailureCount, health.LastFailure.ToString("HH:mm:ss"), health.LastFailureReason ?? "unknown");
                return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// Mark model as failed và calculate recovery time based on API limits
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
            
            // Calculate recovery time based on error type and API limits
            DateTime recoveryTime;
            string limitType;
            string failureReason;
            
            if (ex.Message.Contains("429") || ex.Message.Contains("rate limit"))
            {
                // Rate limit error - use hourly limit reset time
                recoveryTime = _hourlyLimitResetTime;
                limitType = "hourly_rate_limit";
                failureReason = "429_RATE_LIMIT";
                
                _logger.Warning("🚫 Model {Model} rate limited (count: {Count}) - will recover at {RecoveryTime} (hourly limit)", 
                    modelName, health.FailureCount, recoveryTime.ToString("HH:mm:ss UTC"));
            }
            else if (ex.Message.Contains("404_MODEL_NOT_FOUND"))
            {
                // 404 - Model not found - permanently disable
                recoveryTime = DateTime.MaxValue; // Never recover
                limitType = "model_not_found";
                failureReason = "404_MODEL_NOT_FOUND";
                
                _logger.Warning("🚫 Model {Model} not found (404) - permanently disabled", modelName);
            }
            else if (ex.Message.Contains("quota exceeded") || ex.Message.Contains("daily limit"))
            {
                // Daily quota exceeded - use daily limit reset time
                recoveryTime = _dailyLimitResetTime;
                limitType = "daily_quota";
                failureReason = "DAILY_QUOTA_EXCEEDED";
                
                _logger.Warning("📊 Model {Model} daily quota exceeded (count: {Count}) - will recover at {RecoveryTime} (daily limit)", 
                    modelName, health.FailureCount, recoveryTime.ToString("yyyy-MM-dd HH:mm:ss UTC"));
            }
            else if (ex.Message.Contains("500") || ex.Message.Contains("502") || ex.Message.Contains("503"))
            {
                // Server error - use shorter recovery time
                recoveryTime = DateTime.UtcNow.AddMinutes(MODEL_RECOVERY_MINUTES);
                limitType = "server_error";
                failureReason = "5XX_SERVER_ERROR";
                
                _logger.Warning("🔧 Model {Model} server error (count: {Count}) - will recover at {RecoveryTime} (temporary)", 
                    modelName, health.FailureCount, recoveryTime.ToString("HH:mm:ss UTC"));
            }
            else
            {
                // Other error - use default recovery time
                recoveryTime = DateTime.UtcNow.AddMinutes(MODEL_RECOVERY_MINUTES);
                limitType = "other_error";
                failureReason = "OTHER_ERROR";
                
                _logger.Warning("❌ Model {Model} failed (count: {Count}): {Error} - will recover at {RecoveryTime}", 
                    modelName, health.FailureCount, ex.Message, recoveryTime.ToString("HH:mm:ss UTC"));
            }
            
            health.LastFailureReason = failureReason;
            health.RecoveryTime = recoveryTime;
            health.LimitType = limitType;
        }
        
        // Trigger save to persist the failure
        TrySaveModelHealth();
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
            health.LastFailureReason = null;
            health.RecoveryTime = null;
            health.LimitType = null;
        }
        _logger.Information("🔄 All model failures reset - fresh start for all models");
        
        // Trigger save to persist the reset
        TrySaveModelHealth();
    }

    /// <summary>
    /// Enhanced API call với rotation và smart retry
    /// </summary>
    public async Task<string> CallGeminiAPIAsync(string prompt, int maxTokens = 4000)
    {
        // Store current prompt for UI display
        _currentPrompt = prompt;
        _currentPromptTimestamp = DateTime.UtcNow;
        
        // Check daily/hourly limits only (no rate limiting)
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
                _logger.Information("⏰ API call delayed - Hourly limit. Waiting {WaitSeconds} seconds until {NextTime}",
                    Math.Ceiling(waitTime.TotalSeconds), nextCallableTime.ToString("HH:mm:ss UTC"));
                await Task.Delay(waitTime);
            }
        }

        // Initialize model usage tracking if needed
        lock (_modelRotationLock)
        {
            foreach (var model in _geminiFlashModels)
            {
                if (!_lastModelUsage.ContainsKey(model.ModelName))
                {
                    _lastModelUsage[model.ModelName] = DateTime.MinValue;
                }
            }
        }

        // Increment API call counts
        lock (_limitLock)
        {
            _dailyCallCount++;
            _hourlyCallCount++;
            _logger.Information("📊 API usage: {HourlyCount}/{HourlyLimit} per hour, {DailyCount}/{DailyLimit} per day", 
                _hourlyCallCount, _hourlyCallLimit, _dailyCallCount, _dailyCallLimit);
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
                _currentModelUsed = currentModel; // Store for UI display
                var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{currentModel}:generateContent?key={_apiKey}";
                
                try
                {
                    _logger.Information("🚀 Making Gemini API call với model: {Model} (attempt {Attempt}/{MaxRetries})", 
                        currentModel, retry + 1, maxRetries);
                    
                    var response = await _httpClient.PostAsync(requestUrl, content);
                    // Track model usage for rotation
                    lock (_modelRotationLock)
                    {
                        _lastModelUsage[currentModel] = DateTime.UtcNow;
                    }

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
                        
                        // Mark model as failed for various error types
                        if ((int)response.StatusCode == 404)
                        {
                            // 404 - Model not found - mark as permanently failed
                            MarkModelAsFailed(currentModel, new Exception($"404_MODEL_NOT_FOUND: {currentModel} is not available"));
                            _logger.Warning("🚫 Model {Model} not found (404) - will be permanently skipped", currentModel);
                        }
                        else if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                        {
                            MarkModelAsFailed(currentModel, exception);
                            
                            // For rate limit errors, log specific message
                            if ((int)response.StatusCode == 429)
                            {
                                _logger.Warning("🚫 Model {Model} rate limited - will be skipped for extended period", currentModel);
                            }
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
        // No finally block needed - no semaphore to release

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
        lock (_limitLock)
        {
            // Check if limits need reset
            if (DateTime.UtcNow >= _dailyLimitResetTime)
            {
                ResetDailyLimits();
            }
            if (DateTime.UtcNow >= _hourlyLimitResetTime)
            {
                ResetHourlyLimits();
            }

            // Check daily quota
            if (_dailyCallCount >= _dailyCallLimit)
            {
                _logger.Warning("🚫 Daily API limit reached: {CurrentCount}/{DailyLimit}. Next reset: {ResetTime}",
                    _dailyCallCount, _dailyCallLimit, _dailyLimitResetTime.ToString("yyyy-MM-dd HH:mm:ss UTC"));
                return false;
            }

            // Check hourly quota
            if (_hourlyCallCount >= _hourlyCallLimit)
            {
                _logger.Warning("🚫 Hourly API limit reached: {CurrentCount}/{HourlyLimit}. Next reset: {ResetTime}",
                    _hourlyCallCount, _hourlyCallLimit, _hourlyLimitResetTime.ToString("HH:mm:ss UTC"));
                return false;
            }

            // Check model rotation cooldown (no rate limiting, just model rotation)
            var availableModels = _geminiFlashModels.Where(m => IsModelHealthy(m.ModelName)).ToList();
            var readyModels = availableModels.Where(m => 
            {
                var lastUsage = _lastModelUsage.GetValueOrDefault(m.ModelName, DateTime.MinValue);
                return DateTime.UtcNow - lastUsage >= _modelCooldownPeriod;
            }).ToList();
            
            if (!readyModels.Any())
            {
                var nextReadyTime = availableModels.Min(m => 
                    _lastModelUsage.GetValueOrDefault(m.ModelName, DateTime.MinValue) + _modelCooldownPeriod);
                var waitTime = nextReadyTime - DateTime.UtcNow;
                _logger.Information("🔄 Model rotation: waiting {DelayMs}ms for next available model",
                    waitTime.TotalMilliseconds);
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
    /// Get current prompt and model info for UI display
    /// </summary>
    public (string ModelName, string Prompt, DateTime Timestamp) GetCurrentPromptInfo()
    {
        return (_currentModelUsed, _currentPrompt, _currentPromptTimestamp);
    }

    /// <summary>
    /// Get the next time when API call will be available
    /// </summary>
    public DateTime GetNextCallableTime()
    {
        lock (_limitLock)
        {
            // Check if limits need reset
            if (DateTime.UtcNow >= _dailyLimitResetTime)
            {
                ResetDailyLimits();
            }
            if (DateTime.UtcNow >= _hourlyLimitResetTime)
            {
                ResetHourlyLimits();
            }

            // If daily limit reached, return reset time (next day)
            if (_dailyCallCount >= _dailyCallLimit)
            {
                return _dailyLimitResetTime;
            }

            // If hourly limit reached, return reset time (next hour)
            if (_hourlyCallCount >= _hourlyCallLimit)
            {
                return _hourlyLimitResetTime;
            }

            // Otherwise, return time based on model rotation cooldown
            var availableModels = _geminiFlashModels.Where(m => IsModelHealthy(m.ModelName)).ToList();
            if (availableModels.Any())
            {
                var nextReadyTime = availableModels.Min(m => 
                    _lastModelUsage.GetValueOrDefault(m.ModelName, DateTime.MinValue) + _modelCooldownPeriod);
                return nextReadyTime > DateTime.UtcNow ? nextReadyTime : DateTime.UtcNow;
            }
            return DateTime.UtcNow;
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
    /// Reset hourly limits (called every hour)
    /// </summary>
    private void ResetHourlyLimits()
    {
        var oldCount = _hourlyCallCount;
        _hourlyCallCount = 0;
        _hourlyLimitResetTime = DateTime.UtcNow.AddHours(1); // Next hour
        
        _logger.Information("🔄 Hourly API limits reset. Previous count: {OldCount}, Next reset: {NextReset}",
            oldCount, _hourlyLimitResetTime.ToString("HH:mm:ss UTC"));
    }

    /// <summary>
    /// Get current API usage statistics
    /// </summary>
    public Dictionary<string, object> GetAPIUsageStatistics()
    {
        lock (_limitLock)
        {
            // Check if limits need reset
            if (DateTime.UtcNow >= _dailyLimitResetTime)
            {
                ResetDailyLimits();
            }
            if (DateTime.UtcNow >= _hourlyLimitResetTime)
            {
                ResetHourlyLimits();
            }

            return new Dictionary<string, object>
            {
                ["HourlyCallsUsed"] = _hourlyCallCount,
                ["HourlyCallLimit"] = _hourlyCallLimit,
                ["HourlyCallsRemaining"] = Math.Max(0, _hourlyCallLimit - _hourlyCallCount),
                ["HourlyResetTime"] = _hourlyLimitResetTime.ToString("HH:mm:ss UTC"),
                ["DailyCallsUsed"] = _dailyCallCount,
                ["DailyCallLimit"] = _dailyCallLimit,
                ["DailyCallsRemaining"] = Math.Max(0, _dailyCallLimit - _dailyCallCount),
                ["DailyResetTime"] = _dailyLimitResetTime.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ["LastAPICall"] = "N/A (Model rotation mode)",
                ["NextCallableTime"] = GetNextCallableTime().ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ["CanCallNow"] = CanCallAPINow(),
                ["ModelCooldownPeriod"] = _modelCooldownPeriod.TotalSeconds + " seconds"
            };
        }
    }

    public void Dispose()
    {
        // Save model health data before disposing
        try
        {
            SaveModelHealthToFile();
            _logger.Information("💾 Model health data saved on disposal");
        }
        catch (Exception ex)
        {
            _logger.Warning("⚠️ Failed to save model health data on disposal: {Error}", ex.Message);
        }
        
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
    public string? LastFailureReason { get; set; }  // Store failure reason for better recovery logic
    public DateTime? RecoveryTime { get; set; }     // Calculated recovery time based on API limits
    public string? LimitType { get; set; }          // Type of limit hit: "hourly", "daily", "rate_limit", "server_error"
}

#endregion 