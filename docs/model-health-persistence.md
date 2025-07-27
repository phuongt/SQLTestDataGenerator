# Model Health Persistence Enhancement

## 🎯 Vấn Đề Gốc

Trước đây, model health data chỉ lưu trong memory:
- **Khi restart app**: Mất hết thông tin recovery time
- **Không persist**: Model bị rate limit sẽ reset về healthy
- **Không tracking**: Không biết model nào đã bị limit trước đó

**Vấn đề**: Recovery time bị mất khi restart, dẫn đến retry không cần thiết.

## ✅ Giải Pháp Đã Áp Dụng

### 1. **Model Health File Persistence**
**File**: `SqlTestDataGenerator.Core/Services/EnhancedGeminiFlashRotationService.cs`

#### Thêm persistence configuration:
```csharp
// Model Health Persistence
private static readonly string _modelHealthFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "model-health.json");
private static readonly object _persistenceLock = new object();
private static DateTime _lastHealthSave = DateTime.MinValue;
private static readonly TimeSpan _healthSaveInterval = TimeSpan.FromMinutes(1); // Save every minute
```

### 2. **Load Model Health on Startup**
**File**: `SqlTestDataGenerator.Core/Services/EnhancedGeminiFlashRotationService.cs`

#### Cải thiện constructor:
```csharp
public EnhancedGeminiFlashRotationService(string apiKey)
{
    _logger = Log.Logger.ForContext<EnhancedGeminiFlashRotationService>();
    _httpClient = new HttpClient();
    _httpClient.Timeout = TimeSpan.FromMinutes(3);
    _apiKey = apiKey;
    
    LoadModelHealthFromFile(); // Load persisted health data
    InitializeModelHealth();
    
    // ... rest of initialization
    _logger.Information("💾 Model health persistence: {HealthFile}", _modelHealthFile);
}
```

### 3. **Load Model Health Data**
**File**: `SqlTestDataGenerator.Core/Services/EnhancedGeminiFlashRotationService.cs`

#### Method LoadModelHealthFromFile:
```csharp
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
```

### 4. **Save Model Health Data**
**File**: `SqlTestDataGenerator.Core/Services/EnhancedGeminiFlashRotationService.cs`

#### Method SaveModelHealthToFile:
```csharp
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
```

### 5. **Automatic Save Triggers**
**File**: `SqlTestDataGenerator.Core/Services/EnhancedGeminiFlashRotationService.cs`

#### Cải thiện MarkModelAsFailed:
```csharp
public void MarkModelAsFailed(string modelName, Exception ex)
{
    // ... existing failure logic ...
    
    health.LastFailureReason = failureReason;
    health.RecoveryTime = recoveryTime;
    health.LimitType = limitType;
    
    // Trigger save to persist the failure
    TrySaveModelHealth();
}
```

#### Cải thiện IsModelHealthy (khi model recovered):
```csharp
private bool IsModelHealthy(string modelName)
{
    // ... existing health check logic ...
    
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
}
```

### 6. **Save on Disposal**
**File**: `SqlTestDataGenerator.Core/Services/EnhancedGeminiFlashRotationService.cs`

#### Cải thiện Dispose:
```csharp
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
```

### 7. **Public Methods for Management**
**File**: `SqlTestDataGenerator.Core/Services/EnhancedGeminiFlashRotationService.cs`

#### Thêm public methods:
```csharp
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
```

## 📁 File Structure

### Model Health File Location:
```
{AppBaseDirectory}/data/model-health.json
```

### Example File Content:
```json
{
  "gemini-2.0-flash": {
    "FailureCount": 3,
    "LastFailure": "2025-01-25T13:45:30.123Z",
    "IsHealthy": false,
    "LastFailureReason": "429_RATE_LIMIT",
    "RecoveryTime": "2025-01-25T14:00:00.000Z",
    "LimitType": "hourly_rate_limit"
  },
  "gemini-1.5-flash": {
    "FailureCount": 0,
    "LastFailure": "0001-01-01T00:00:00.000Z",
    "IsHealthy": true,
    "LastFailureReason": null,
    "RecoveryTime": null,
    "LimitType": null
  }
}
```

## 🧪 Testing

### Test Script:
**File**: `scripts/test-model-health-persistence.ps1`

#### Usage:
```powershell
# Test persistence with clean start
.\scripts\test-model-health-persistence.ps1 -CleanStart

# Test persistence with verbose logging
.\scripts\test-model-health-persistence.ps1 -Verbose

# Normal test
.\scripts\test-model-health-persistence.ps1
```

#### Test Coverage:
1. **Health file creation**: Kiểm tra file được tạo
2. **Persistence across restarts**: Kiểm tra data persist
3. **File permissions**: Kiểm tra read/write access
4. **Data structure**: Kiểm tra JSON format
5. **Recovery time validation**: Kiểm tra expired recovery times

## 📊 Persistence Logic

### 1. **Save Triggers**
| Event | Trigger | Frequency |
|-------|---------|-----------|
| **Model Failed** | `MarkModelAsFailed()` | Immediate |
| **Model Recovered** | `IsModelHealthy()` | Immediate |
| **All Models Reset** | `ResetAllModelFailures()` | Immediate |
| **App Shutdown** | `Dispose()` | On exit |
| **Periodic Save** | `TrySaveModelHealth()` | Every 1 minute |

### 2. **Load Triggers**
| Event | Trigger | Logic |
|-------|---------|-------|
| **App Startup** | Constructor | Load from file |
| **Manual Reload** | `ForceReloadModelHealth()` | User request |

### 3. **Data Validation**
| Validation | Logic | Action |
|------------|-------|--------|
| **File not exists** | Start fresh | Initialize empty |
| **JSON parse error** | Clear data | Start fresh |
| **Expired recovery time** | Reset model | Mark as healthy |
| **Missing models** | Add defaults | Initialize health |

## 🚀 Kết Quả Mong Đợi

### Performance Improvement:
- **Persistent recovery time**: Không mất khi restart
- **Accurate model selection**: Dựa trên health data thực tế
- **Reduced unnecessary retries**: Không retry model đang recovery
- **Better resource utilization**: Sử dụng healthy models

### Logging Enhancement:
```
📁 No existing model health file found. Starting fresh.
📂 Loaded model health data: 8 models, 6 healthy
🔄 Model gemini-2.0-flash recovery time expired, resetting health
💾 Model health data saved: 8 models
💾 Model health data saved on disposal
```

### File Management:
```
📁 File location: C:\App\data\model-health.json
📅 Created: 2025-01-25 13:30:00
📅 Modified: 2025-01-25 13:45:30
📊 Size: 1,234 bytes
✅ File is readable and writable
```

## 🔧 Monitoring

### Health Check Commands:
```csharp
// Get health file path
var healthFilePath = flashRotationService.GetModelHealthFilePath();
Console.WriteLine($"Health file: {healthFilePath}");

// Force save health data
flashRotationService.ForceSaveModelHealth();

// Force reload health data
flashRotationService.ForceReloadModelHealth();

// Get model statistics
var modelStats = flashRotationService.GetModelStatistics();
Console.WriteLine($"Healthy models: {modelStats["HealthyModels"]}/{modelStats["TotalModels"]}");
```

### File Monitoring:
```powershell
# Check health file
Get-Content "data/model-health.json" | ConvertFrom-Json

# Monitor file changes
Get-ChildItem "data/model-health.json" | Select-Object Name, Length, LastWriteTime

# Check file permissions
Get-Acl "data/model-health.json"
```

## 📋 Best Practices

### 1. **File Management**
- **Backup regularly**: Copy health file before updates
- **Monitor disk space**: Health file grows with model count
- **Check permissions**: Ensure app can read/write data directory
- **Validate JSON**: Check file integrity periodically

### 2. **Recovery Time Management**
- **Monitor expired times**: Check for stale recovery data
- **Validate limits**: Ensure recovery times match API limits
- **Reset if needed**: Clear invalid recovery times

### 3. **Performance Optimization**
- **Save interval**: Adjust `_healthSaveInterval` based on usage
- **File size**: Monitor health file size growth
- **Memory usage**: Health data stays in memory

## 🎯 Kết Luận

Việc cải thiện này đã:
- ✅ **Persistent recovery time**: Lưu vào file, không mất khi restart
- ✅ **Accurate model selection**: Dựa trên health data thực tế
- ✅ **Reduced unnecessary retries**: Không retry model đang recovery
- ✅ **Better resource utilization**: Sử dụng healthy models
- ✅ **Enhanced monitoring**: File-based health tracking
- ✅ **Atomic file operations**: Prevent corruption during saves

**Bây giờ AI service sẽ nhớ recovery time của các model giữa các lần restart app, giúp tránh retry không cần thiết và tối ưu performance!** 🎉 