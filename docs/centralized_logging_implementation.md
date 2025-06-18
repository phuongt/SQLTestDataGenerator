# Centralized Logging Implementation

## Tổng Quan

Hệ thống **Centralized Logging** được thiết kế để thống nhất tất cả logs từ UI, Tests, và Engine components vào **1 thư mục duy nhất** `/logs/` thay vì scattered across multiple locations.

## Vấn Đề Trước Đây

### 🔴 Logs Scattered
- `/logs/` - UI/Manual runs
- `/SqlTestDataGenerator.Tests/logs/` - Test logs  
- `/SqlTestDataGenerator.UI/bin/Debug/net8.0/logs/` - UI bin logs
- Multiple configurations và inconsistent paths

### 🔴 Problems
- Khó troubleshoot khi logs ở nhiều chỗ
- Configuration không consistent
- Manual cleanup required cho từng folder
- Development experience poor

## Giải Pháp Centralized

### ✅ Single Logs Location
```
/logs/
├── app.log                    # UI logs
├── engine-20250617.log        # Engine logs (daily rotation)
├── test-20250617-143022.log   # Test logs (timestamped)
├── ai-20250617.log            # AI API logs
└── old logs...                # Auto-cleanup after 30 days
```

### ✅ Benefits
- **Single location** để check tất cả logs
- **Consistent naming** conventions
- **Auto-cleanup** old logs
- **Smart categorization** by component
- **Easy troubleshooting** workflow

## Architecture

### Core Components

1. **LoggingConfiguration** - Centralized config model
2. **CentralizedLoggingManager** - Main orchestrator
3. **LogComponent Enum** - Component categorization
4. **LogsSummary** - Analytics và reporting

### Smart Path Resolution

```csharp
// For test projects running from bin/Debug
if (currentDir.Contains("bin")) {
    // Navigate to solution root: TestProject -> Solution -> /logs/
    var solutionRoot = Directory.GetParent(projectRoot)?.FullName;
    return Path.Combine(solutionRoot, "logs");
}

// For UI/direct runs
return Path.Combine(currentDir, "logs");
```

## Configuration Updates

### 1. Test Configuration
**File**: `SqlTestDataGenerator.Tests/app.config`
```xml
<!-- OLD -->
<add key="LogFilePath" value="logs/ai-test-.txt" />

<!-- NEW: Points to root logs directory -->
<add key="LogFilePath" value="../logs/test-ai-.txt" />
```

### 2. UI Configuration  
**File**: `SqlTestDataGenerator.UI/appsettings.json`
```json
{
  "Logging": {
    "FilePath": "logs/app.log",    // Already correct
    "EnableUILogging": true
  }
}
```

### 3. Engine Configuration
**File**: `EngineService.cs`
```csharp
// OLD: Manual directory creation
var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");

// NEW: Centralized manager
CentralizedLoggingManager.Initialize();
var logsDir = CentralizedLoggingManager.GetLogsDirectory();
```

## Log File Naming Patterns

### Component-Based Naming
```csharp
public class LogFileNaming
{
    public string UI { get; set; } = "app.log";
    public string Engine { get; set; } = "engine-{date}.log";
    public string Tests { get; set; } = "test-{timestamp}.log";
    public string AI { get; set; } = "ai-{date}.log";
}
```

### Pattern Variables
- `{date}` → `yyyyMMdd` (e.g., `20250617`)
- `{timestamp}` → `yyyyMMdd-HHmmss` (e.g., `20250617-143022`)

## Usage Examples

### Create Logger for Specific Component
```csharp
// Initialize centralized logging
CentralizedLoggingManager.Initialize();

// Create logger for specific component
var uiLogger = new LoggerService(
    CentralizedLoggingManager.CreateLoggingSettings(LogComponent.UI)
);

var testLogger = new LoggerService(
    CentralizedLoggingManager.CreateLoggingSettings(LogComponent.Tests)
);
```

### Get Logs Summary
```csharp
var summary = CentralizedLoggingManager.GetLogsSummary();
Console.WriteLine(summary);

// Output:
// Logs Directory: C:\Customize\04.GenData\logs
// Total Files: 8
// Total Size: 15.67 MB
// UI: 1, Engine: 3, Test: 2, AI: 1, Other: 1
```

### Auto-Cleanup Old Logs
```csharp
// Manual cleanup
await CentralizedLoggingManager.AutoCleanupOldLogsAsync();

// Auto cleanup (runs during initialization if configured)
var config = new LoggingConfiguration 
{ 
    AutoCleanupAfterDays = 30 
};
CentralizedLoggingManager.Initialize(config);
```

## Verification & Testing

### PowerShell Verification Script
```powershell
# Basic verification
.\scripts\verify-centralized-logs.ps1

# Show detailed file analysis
.\scripts\verify-centralized-logs.ps1 -ShowDetails

# Clean up old scattered logs
.\scripts\verify-centralized-logs.ps1 -CleanupOldLogs

# Test new log generation
.\scripts\verify-centralized-logs.ps1 -TestGeneration
```

### Manual Verification Steps

1. **Check Main Directory**
   ```powershell
   ls logs/*.log | measure-object
   ```

2. **Verify No Scattered Logs**
   ```powershell
   ls SqlTestDataGenerator.Tests/logs/*.log -ErrorAction SilentlyContinue
   ls SqlTestDataGenerator.UI/bin/Debug/net8.0/logs/*.log -ErrorAction SilentlyContinue
   ```

3. **Test Log Generation**
   ```powershell
   dotnet test SqlTestDataGenerator.Tests --filter "TestMethod=DatabaseConnectionTest"
   # Check if new logs appear in /logs/
   ```

## Migration Guide

### Step 1: Run Verification
```powershell
.\scripts\verify-centralized-logs.ps1 -ShowDetails
```

### Step 2: Migrate Old Logs
```powershell
.\scripts\verify-centralized-logs.ps1 -CleanupOldLogs
```

### Step 3: Test New Logging
```powershell
.\scripts\verify-centralized-logs.ps1 -TestGeneration
```

### Step 4: Verify Success
```powershell
.\scripts\verify-centralized-logs.ps1
# Should show: "🎉 CENTRALIZED LOGGING SUCCESSFULLY CONFIGURED!"
```

## Troubleshooting

### Issue: Logs Still Scattered
```powershell
# Check configuration
cat SqlTestDataGenerator.Tests/app.config | findstr LogFilePath
cat SqlTestDataGenerator.UI/appsettings.json | findstr FilePath

# Manual migration
mv SqlTestDataGenerator.Tests/logs/*.log logs/
mv SqlTestDataGenerator.UI/bin/Debug/net8.0/logs/*.log logs/
```

### Issue: Logs Not Created
```csharp
// Debug path resolution
Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Logs Directory: {CentralizedLoggingManager.GetLogsDirectory()}");

// Force initialization
CentralizedLoggingManager.Initialize(new LoggingConfiguration 
{
    LogsDirectory = "logs"
});
```

### Issue: Permission Errors
```powershell
# Check permissions
icacls logs
# Fix permissions if needed
icacls logs /grant Everyone:F
```

## Performance Benefits

### Before Centralization
- ❌ Multiple I/O operations across directories
- ❌ Manual log rotation per location  
- ❌ Scattered file handles
- ❌ Complex troubleshooting

### After Centralization  
- ✅ Single I/O location with optimized access
- ✅ Centralized rotation and cleanup
- ✅ Unified file handle management
- ✅ Simple troubleshooting workflow

## Future Enhancements

### Phase 1 (Current)
- ✅ Centralized location
- ✅ Component categorization
- ✅ Auto-cleanup
- ✅ Smart path resolution

### Phase 2 (Planned)
- 🔄 Real-time log streaming to UI
- 🔄 Log filtering by component/level
- 🔄 Export logs for specific time ranges
- 🔄 Compression for old log files

### Phase 3 (Future)
- 🔄 Structured logging with JSON format
- 🔄 Log aggregation and analytics
- 🔄 Integration with external log systems
- 🔄 Log-based monitoring and alerts

## Best Practices

### ✅ Do
- Use `CentralizedLoggingManager.Initialize()` at application start
- Use appropriate `LogComponent` enum for categorization
- Let auto-cleanup handle old files
- Use verification script before deployments

### ❌ Don't
- Create logs in arbitrary locations
- Hardcode log paths
- Skip initialization step
- Ignore cleanup warnings

## Summary

**Centralized Logging** đã thành công thống nhất tất cả logs vào `/logs/` directory với:

- **Simplified Management**: 1 location thay vì 3+
- **Smart Organization**: Component-based categorization
- **Auto-Maintenance**: Cleanup và rotation
- **Better DX**: Easy troubleshooting workflow
- **Production Ready**: Robust path resolution 