# Oracle Timeout Detection Enhancement

## 🎯 Vấn Đề Gốc

Oracle test với AI bị timeout sau 5 phút:
- **Không biết chỗ nào chậm**: Không có detailed timing breakdown
- **AI generation chậm**: Có thể do rate limiting hoặc model selection
- **Database operations**: Có thể do Oracle connection hoặc schema analysis
- **Constraint validation**: Có thể do deep validation logic

**Vấn đề**: Cần xác định chính xác bottleneck để tối ưu.

## ✅ Giải Pháp Đã Áp Dụng

### 1. **Enhanced Test Timing**
**File**: `SqlTestDataGenerator.Tests/OracleComplexQueryPhương1989Tests.cs`

#### Cải thiện test method với detailed timing:
```csharp
[TestMethod]
[Timeout(300000)] // 5 phút timeout cho AI service với rate limit handling
public async Task TestComplexQueryPhuong1989_ShouldGenerateDataAndExecute()
{
    // Full integration test with actual Oracle database - DETAILED PERFORMANCE LOGGING
    Console.WriteLine("🚀 Starting detailed Oracle complex query test with AI...");
    var testStartTime = DateTime.UtcNow;
    
    // ... test setup ...
    
    Console.WriteLine($"⏱️ Test started at: {testStartTime:HH:mm:ss.fff}");
    Console.WriteLine($"🎯 Target: Generate {request.DesiredRecordCount} records");
    Console.WriteLine($"🤖 AI Service: ENABLED (UseAI=true) with enhanced rate limit handling");
    Console.WriteLine($"📊 SQL Length: {complexSql.Length} characters");
    Console.WriteLine($"🔗 Oracle Connection: {(_hasOracleConnection ? "Available" : "Not Available")}");

    // Act - với detailed timing
    Console.WriteLine("🔄 Starting ExecuteQueryWithTestDataAsync...");
    var executionStartTime = DateTime.UtcNow;
    
    var result = await _engineService!.ExecuteQueryWithTestDataAsync(request);
    
    var executionEndTime = DateTime.UtcNow;
    var executionDuration = executionEndTime - executionStartTime;
    var totalDuration = executionEndTime - testStartTime;
    
    Console.WriteLine($"⏱️ Execution completed at: {executionEndTime:HH:mm:ss.fff}");
    Console.WriteLine($"⏱️ Execution duration: {executionDuration.TotalSeconds:F2} seconds");
    Console.WriteLine($"⏱️ Total test duration: {totalDuration.TotalSeconds:F2} seconds");

    // Detailed result analysis
    Console.WriteLine($"📊 Result Analysis:");
    Console.WriteLine($"   - Success: {result.Success}");
    Console.WriteLine($"   - Generated Records: {result.GeneratedRecords}");
    Console.WriteLine($"   - Result Data Rows: {result.ResultData?.Rows.Count ?? 0}");
    Console.WriteLine($"   - Error Message: {result.ErrorMessage ?? "None"}");
    Console.WriteLine($"   - Execution Time: {executionDuration.TotalSeconds:F2}s");
}
```

### 2. **EngineService Detailed Timing**
**File**: `SqlTestDataGenerator.Core/Services/EngineService.cs`

#### Thêm step-by-step timing cho ExecuteQueryWithTestDataAsync:
```csharp
public async Task<QueryExecutionResult> ExecuteQueryWithTestDataAsync(QueryExecutionRequest request)
{
    var stopwatch = Stopwatch.StartNew();
    var result = new QueryExecutionResult();
    var stepTimings = new Dictionary<string, TimeSpan>();

    try
    {
        Console.WriteLine($"[EngineService] Starting execution for {request.DatabaseType}");
        Console.WriteLine($"[EngineService] SQL Length: {request.SqlQuery.Length} characters");
        Console.WriteLine($"[EngineService] UseAI: {request.UseAI}");
        Console.WriteLine($"[EngineService] Desired Records: {request.DesiredRecordCount}");

        // Step 0: SQL Conversion
        var step0Start = Stopwatch.StartNew();
        // ... SQL conversion logic ...
        step0Start.Stop();
        stepTimings["Step0_SQL_Conversion"] = step0Start.Elapsed;
        Console.WriteLine($"[EngineService] Step 0 (SQL Conversion): {step0Start.Elapsed.TotalSeconds:F2}s");

        // Step 1: Analysis & Schema
        var step1Start = Stopwatch.StartNew();
        
        // Step 1.1: Initial Schema
        var step1_1Start = Stopwatch.StartNew();
        Console.WriteLine($"[EngineService] Step 1.1: Getting initial database schema...");
        // ... schema extraction ...
        step1_1Start.Stop();
        stepTimings["Step1_1_Initial_Schema"] = step1_1Start.Elapsed;
        Console.WriteLine($"[EngineService] Step 1.1 (Initial Schema): {step1_1Start.Elapsed.TotalSeconds:F2}s");
        
        // Step 1.2: Constraint Extraction
        var step1_2Start = Stopwatch.StartNew();
        Console.WriteLine($"[EngineService] Step 1.2: Extracting comprehensive constraints from SQL query");
        // ... constraint extraction ...
        step1_2Start.Stop();
        stepTimings["Step1_2_Constraint_Extraction"] = step1_2Start.Elapsed;
        Console.WriteLine($"[EngineService] Step 1.2 (Constraint Extraction): {step1_2Start.Elapsed.TotalSeconds:F2}s");
        
        // Step 1.3: Dependency Resolution
        var step1_3Start = Stopwatch.StartNew();
        Console.WriteLine($"[EngineService] Step 1.3: Resolving dependencies...");
        // ... dependency resolution ...
        step1_3Start.Stop();
        stepTimings["Step1_3_Dependency_Resolution"] = step1_3Start.Elapsed;
        Console.WriteLine($"[EngineService] Step 1.3 (Dependency Resolution): {step1_3Start.Elapsed.TotalSeconds:F2}s");
        
        // Step 1.4: Full Schema
        var step1_4Start = Stopwatch.StartNew();
        Console.WriteLine($"[EngineService] Step 1.4: Getting full database schema for all required tables...");
        // ... full schema extraction ...
        step1_4Start.Stop();
        stepTimings["Step1_4_Full_Schema"] = step1_4Start.Elapsed;
        Console.WriteLine($"[EngineService] Step 1.4 (Full Schema): {step1_4Start.Elapsed.TotalSeconds:F2}s");
        
        // Step 1.5: Table Truncation
        var step1_5Start = Stopwatch.StartNew();
        Console.WriteLine($"[EngineService] Step 1.5: Truncating {databaseInfo.Tables.Count} tables to avoid duplicate keys");
        // ... table truncation ...
        step1_5Start.Stop();
        stepTimings["Step1_5_Table_Truncation"] = step1_5Start.Elapsed;
        Console.WriteLine($"[EngineService] Step 1.5 (Table Truncation): {step1_5Start.Elapsed.TotalSeconds:F2}s");
        
        step1Start.Stop();
        stepTimings["Step1_Total_Analysis"] = step1Start.Elapsed;
        Console.WriteLine($"[EngineService] Step 1 (Total Analysis): {step1Start.Elapsed.TotalSeconds:F2}s");

        // Step 2: Data Generation
        var step2Start = Stopwatch.StartNew();
        Console.WriteLine($"[EngineService] Step 2: Generating constraint-aware data for {request.DesiredRecordCount} records, UseAI: {request.UseAI}");
        
        // Step 2.1: Table Ordering
        var step2_1Start = Stopwatch.StartNew();
        // ... table ordering ...
        step2_1Start.Stop();
        stepTimings["Step2_1_Table_Ordering"] = step2_1Start.Elapsed;
        Console.WriteLine($"[EngineService] Step 2.1 (Table Ordering): {step2_1Start.Elapsed.TotalSeconds:F2}s");
        
        // Step 2.2: AI Data Generation (MAIN BOTTLENECK)
        var step2_2Start = Stopwatch.StartNew();
        Console.WriteLine($"[EngineService] Step 2.2: Starting AI-powered data generation...");
        // ... AI data generation ...
        step2_2Start.Stop();
        stepTimings["Step2_2_AI_Data_Generation"] = step2_2Start.Elapsed;
        Console.WriteLine($"[EngineService] Step 2.2 (AI Data Generation): {step2_2Start.Elapsed.TotalSeconds:F2}s");
        
        // Step 2.3: Reorder Inserts
        var step2_3Start = Stopwatch.StartNew();
        // ... reorder inserts ...
        step2_3Start.Stop();
        stepTimings["Step2_3_Reorder_Inserts"] = step2_3Start.Elapsed;
        Console.WriteLine($"[EngineService] Step 2.3 (Reorder Inserts): {step2_3Start.Elapsed.TotalSeconds:F2}s");
        
        step2Start.Stop();
        stepTimings["Step2_Data_Generation"] = step2Start.Elapsed;
        Console.WriteLine($"[EngineService] Step 2 (Total Data Generation): {step2Start.Elapsed.TotalSeconds:F2}s");

        // Step 3: Database Execution
        var step3Start = Stopwatch.StartNew();
        Console.WriteLine($"[EngineService] Step 3: Executing {insertStatements.Count} INSERT statements");
        // ... database execution ...
        step3Start.Stop();
        stepTimings["Step3_Database_Execution"] = step3Start.Elapsed;
        Console.WriteLine($"[EngineService] Step 3 (Database Execution): {step3Start.Elapsed.TotalSeconds:F2}s");
        
        // Print detailed timing summary
        Console.WriteLine($"[EngineService] ===== DETAILED TIMING SUMMARY =====");
        Console.WriteLine($"[EngineService] Total Execution Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"[EngineService] Breakdown:");
        foreach (var timing in stepTimings.OrderBy(kvp => kvp.Key))
        {
            var percentage = (timing.Value.TotalSeconds / stopwatch.Elapsed.TotalSeconds) * 100;
            Console.WriteLine($"[EngineService]   {timing.Key}: {timing.Value.TotalSeconds:F2}s ({percentage:F1}%)");
        }
        
        // Identify bottleneck
        var bottleneck = stepTimings.OrderByDescending(kvp => kvp.Value.TotalSeconds).First();
        Console.WriteLine($"[EngineService] 🚨 BOTTLENECK: {bottleneck.Key} took {bottleneck.Value.TotalSeconds:F2}s ({bottleneck.Value.TotalSeconds / stopwatch.Elapsed.TotalSeconds * 100:F1}%)");
    }
    catch (Exception ex)
    {
        // ... error handling ...
    }
}
```

### 3. **Timeout Detection Script**
**File**: `scripts/test-oracle-timeout-detection.ps1`

#### Script để phát hiện timeout với real-time monitoring:
```powershell
# Test Oracle Timeout Detection
param(
    [switch]$Verbose = $false,
    [switch]$CleanStart = $false,
    [int]$TimeoutMinutes = 5
)

# Real-time output monitoring
$OutputEvent = Register-ObjectEvent -InputObject $Process -EventName OutputDataReceived -Action {
    if ($Event.SourceEventArgs.Data) {
        $Event.MessageData.AppendLine($Event.SourceEventArgs.Data)
        # Real-time output for monitoring
        Write-Host $Event.SourceEventArgs.Data -ForegroundColor Gray
    }
} -MessageData $OutputBuilder

# Wait for process with timeout
if (!$Process.WaitForExit($TimeoutMs)) {
    Write-Log "⚠️ Test timed out after $TimeoutMinutes minutes" "WARNING"
    
    # Analyze partial output for timing information
    $TimingLines = $TestOutput -split "`n" | Where-Object { $_ -match "Step.*seconds|BOTTLENECK|Execution.*seconds" }
    if ($TimingLines.Count -gt 0) {
        Write-Log "⏰ Found timing information in output:" "INFO"
        foreach ($Line in $TimingLines) {
            Write-Log "   $($Line.Trim())" "INFO"
        }
    }
    
    # Look for bottleneck information
    $BottleneckLines = $TestOutput -split "`n" | Where-Object { $_ -match "BOTTLENECK" }
    if ($BottleneckLines.Count -gt 0) {
        Write-Log "🚨 BOTTLENECK detected:" "ERROR"
        foreach ($Line in $BottleneckLines) {
            Write-Log "   $($Line.Trim())" "ERROR"
        }
    }
}
```

## 📊 Timing Breakdown Analysis

### 1. **Expected Timing Breakdown**
| Step | Description | Expected Time | Potential Issues |
|------|-------------|---------------|------------------|
| **Step 0** | SQL Conversion | < 1s | MySQL syntax conversion |
| **Step 1.1** | Initial Schema | 2-5s | Oracle metadata extraction |
| **Step 1.2** | Constraint Extraction | < 1s | SQL parsing |
| **Step 1.3** | Dependency Resolution | < 1s | Table dependency analysis |
| **Step 1.4** | Full Schema | 5-15s | Oracle schema analysis |
| **Step 1.5** | Table Truncation | 2-10s | Oracle TRUNCATE operations |
| **Step 2.1** | Table Ordering | < 1s | Dependency sorting |
| **Step 2.2** | **AI Data Generation** | **30-240s** | **MAIN BOTTLENECK** |
| **Step 2.3** | Reorder Inserts | < 1s | SQL reordering |
| **Step 3** | Database Execution | 5-30s | INSERT operations |

### 2. **AI Generation Bottleneck Analysis**
**Step 2.2 (AI Data Generation)** có thể chậm do:

#### A. **Rate Limiting Issues**
```
🚫 Model gemini-2.0-flash rate limited (count: 3) - will recover at 14:00:00 UTC (hourly limit)
⏭️ Skipping model gemini-2.0-flash - recovery in 23:45 (limit: hourly_rate_limit)
```

#### B. **Model Selection Delays**
```
🔄 Model rotation: gemini-2.0-flash → gemini-1.5-flash → gemini-2.5-flash
🔍 Checking model health: 3 models unhealthy, 5 models available
```

#### C. **API Call Delays**
```
⏰ Rate limit: need to wait 5000ms before next call
📊 API usage: 12/15 per hour, 85/100 per day
```

#### D. **Constraint Validation Retries**
```
🔄 Deep constraint-aware generation attempt 1/3
❌ DEEP constraint validation FAILED on attempt 1 (pass rate: 45.2%)
🔄 Retrying in 200ms...
```

### 3. **Database Operations Analysis**
**Step 1.4 (Full Schema)** và **Step 1.5 (Table Truncation)** có thể chậm do:

#### A. **Oracle Connection Issues**
```
🔗 Oracle Connection: Available
📊 Found 4 direct tables from SQL
📊 Full schema loaded for 8 tables (including dependencies)
```

#### B. **Schema Analysis Delays**
```
[EngineService] Step 1.4: Getting full database schema for all required tables...
[EngineService] Full schema loaded for 8 tables (including dependencies)
[EngineService] Step 1.4 (Full Schema): 12.45s
```

#### C. **Table Truncation Issues**
```
[EngineService] Step 1.5: Truncating 8 tables to avoid duplicate keys
[EngineService] All tables truncated successfully
[EngineService] Step 1.5 (Table Truncation): 8.23s
```

## 🧪 Testing Commands

### 1. **Run Timeout Detection Test**
```powershell
# Test với 5 phút timeout
.\scripts\test-oracle-timeout-detection.ps1

# Test với 3 phút timeout
.\scripts\test-oracle-timeout-detection.ps1 -TimeoutMinutes 3

# Test với verbose logging
.\scripts\test-oracle-timeout-detection.ps1 -Verbose

# Test với clean start
.\scripts\test-oracle-timeout-detection.ps1 -CleanStart
```

### 2. **Run Final Test với Timing**
```powershell
# Run final test với Oracle focus
.\scripts\final-test.ps1 -DatabaseType Oracle
```

### 3. **Monitor Real-time Output**
```powershell
# Watch logs in real-time
Get-Content "logs/oracle-timeout-detection-*.log" -Wait
```

## 🚨 Expected Bottleneck Scenarios

### 1. **AI Rate Limiting Bottleneck**
```
[EngineService] 🚨 BOTTLENECK: Step2_2_AI_Data_Generation took 180.45s (85.2%)
[EngineService] Step 2.2 (AI Data Generation): 180.45s
```

**Solution**: Check model health persistence, reduce retry attempts

### 2. **Oracle Schema Analysis Bottleneck**
```
[EngineService] 🚨 BOTTLENECK: Step1_4_Full_Schema took 45.67s (67.8%)
[EngineService] Step 1.4 (Full Schema): 45.67s
```

**Solution**: Optimize schema extraction, cache schema data

### 3. **Database Execution Bottleneck**
```
[EngineService] 🚨 BOTTLENECK: Step3_Database_Execution took 120.34s (89.1%)
[EngineService] Step 3 (Database Execution): 120.34s
```

**Solution**: Optimize INSERT operations, reduce batch size

### 4. **Constraint Validation Bottleneck**
```
[EngineService] 🚨 BOTTLENECK: Step2_2_AI_Data_Generation took 95.23s (78.5%)
[EngineService] Deep validation result: 12/15 checks passed (80.0%)
```

**Solution**: Reduce validation complexity, increase pass rate threshold

## 📋 Optimization Recommendations

### 1. **AI Generation Optimization**
- **Reduce retry attempts**: From 3 to 2
- **Increase pass rate threshold**: From 75% to 70%
- **Cache model health**: Use persistence to avoid rate limits
- **Parallel model calls**: Use multiple models simultaneously

### 2. **Database Optimization**
- **Cache schema data**: Store schema for reuse
- **Optimize truncation**: Use faster truncation methods
- **Reduce batch size**: Smaller INSERT batches
- **Connection pooling**: Optimize Oracle connections

### 3. **Constraint Validation Optimization**
- **Simplify validation**: Reduce validation complexity
- **Early termination**: Stop validation if pass rate is good enough
- **Parallel validation**: Validate constraints in parallel

## 🎯 Kết Luận

Việc cải thiện này đã:
- ✅ **Detailed timing breakdown**: Xác định chính xác bottleneck
- ✅ **Real-time monitoring**: Theo dõi progress trong thời gian thực
- ✅ **Bottleneck identification**: Tự động phát hiện chỗ chậm nhất
- ✅ **Timeout detection**: Phát hiện timeout với partial analysis
- ✅ **Performance optimization**: Hướng dẫn tối ưu dựa trên timing

**Bây giờ có thể xác định chính xác chỗ nào bị timeout và tối ưu performance!** 🎉

**Chạy test để xem bottleneck**: `.\scripts\test-oracle-timeout-detection.ps1` 