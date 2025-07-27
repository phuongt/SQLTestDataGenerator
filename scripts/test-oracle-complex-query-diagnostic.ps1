# Oracle Complex Query Diagnostic Test
# Tests the Phương1989 complex SQL query to identify performance bottlenecks and errors

param(
    [string]$TestMode = "full",  # full, quick, or connection-only
    [int]$RecordCount = 5,
    [switch]$EnableAI = $true,
    [switch]$Verbose = $false
)

Write-Host "🔍 Oracle Complex Query Diagnostic Test" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Test Mode: $TestMode" -ForegroundColor Yellow
Write-Host "Record Count: $RecordCount" -ForegroundColor Yellow
Write-Host "AI Enabled: $EnableAI" -ForegroundColor Yellow
Write-Host "Verbose: $Verbose" -ForegroundColor Yellow
Write-Host ""

# Set up logging
$logFile = "logs/oracle-complex-query-diagnostic-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').log"
New-Item -ItemType Directory -Force -Path "logs" | Out-Null

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
    $logMessage = "[$timestamp] [$Level] $Message"
    Write-Host $logMessage
    Add-Content -Path $logFile -Value $logMessage
}

function Test-OracleConnection {
    Write-Log "🔌 Testing Oracle connection..." "CONNECTION"
    $startTime = Get-Date
    
    try {
        $connectionString = "Data Source=localhost:1521/XE;User Id=system;Password=22092012;Connection Timeout=30;Connection Lifetime=60;Pooling=true;Min Pool Size=0;Max Pool Size=5;"
        
        # Test basic connection
        $testResult = & dotnet test SqlTestDataGenerator.Tests --filter "TestOracleConnection" --logger "console;verbosity=detailed" --verbosity normal 2>&1
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        if ($LASTEXITCODE -eq 0) {
            Write-Log "✅ Oracle connection successful in ${duration}s" "CONNECTION"
            return $true
        } else {
            Write-Log "❌ Oracle connection failed after ${duration}s" "CONNECTION"
            Write-Log "Connection test output: $testResult" "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "❌ Oracle connection test exception: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Test-SqlParsing {
    Write-Log "📋 Testing SQL parsing performance..." "PARSING"
    $startTime = Get-Date
    
    try {
        $testResult = & dotnet test SqlTestDataGenerator.Tests --filter "TestOracleTableExtraction_ShouldExtractAllTables" --logger "console;verbosity=detailed" --verbosity normal 2>&1
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        if ($LASTEXITCODE -eq 0) {
            Write-Log "✅ SQL parsing successful in ${duration}s" "PARSING"
            return $true
        } else {
            Write-Log "❌ SQL parsing failed after ${duration}s" "PARSING"
            Write-Log "Parsing test output: $testResult" "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "❌ SQL parsing test exception: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Test-OracleDialectFunctions {
    Write-Log "🔧 Testing Oracle dialect functions..." "DIALECT"
    $startTime = Get-Date
    
    try {
        $testResult = & dotnet test SqlTestDataGenerator.Tests --filter "TestOracleDialectHandling_DateFunctions" --logger "console;verbosity=detailed" --verbosity normal 2>&1
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        if ($LASTEXITCODE -eq 0) {
            Write-Log "✅ Oracle dialect functions successful in ${duration}s" "DIALECT"
            return $true
        } else {
            Write-Log "❌ Oracle dialect functions failed after ${duration}s" "DIALECT"
            Write-Log "Dialect test output: $testResult" "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "❌ Oracle dialect test exception: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Test-CaseStatement {
    Write-Log "📝 Testing Oracle CASE statement..." "CASE"
    $startTime = Get-Date
    
    try {
        $testResult = & dotnet test SqlTestDataGenerator.Tests --filter "TestOracleCaseStatement_WorkStatus" --logger "console;verbosity=detailed" --verbosity normal 2>&1
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        if ($LASTEXITCODE -eq 0) {
            Write-Log "✅ Oracle CASE statement successful in ${duration}s" "CASE"
            return $true
        } else {
            Write-Log "❌ Oracle CASE statement failed after ${duration}s" "CASE"
            Write-Log "CASE test output: $testResult" "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "❌ Oracle CASE test exception: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Test-QuickGeneration {
    Write-Log "⚡ Testing quick data generation (no AI)..." "QUICK"
    $startTime = Get-Date
    
    try {
        $testResult = & dotnet test SqlTestDataGenerator.Tests --filter "TestComplexQueryPhuong1989_ShouldGenerateDataAndExecute" --logger "console;verbosity=detailed" --verbosity normal 2>&1
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        if ($LASTEXITCODE -eq 0) {
            Write-Log "✅ Quick generation successful in ${duration}s" "QUICK"
            return $true
        } else {
            Write-Log "❌ Quick generation failed after ${duration}s" "QUICK"
            Write-Log "Quick test output: $testResult" "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "❌ Quick generation test exception: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Test-AIGeneration {
    Write-Log "🤖 Testing AI-powered data generation..." "AI"
    $startTime = Get-Date
    
    try {
        $testResult = & dotnet test SqlTestDataGenerator.Tests --filter "TestComplexQueryPhuong1989_WithAI_ShouldGenerateDataAndExecute" --logger "console;verbosity=detailed" --verbosity normal 2>&1
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        if ($LASTEXITCODE -eq 0) {
            Write-Log "✅ AI generation successful in ${duration}s" "AI"
            return $true
        } else {
            Write-Log "❌ AI generation failed after ${duration}s" "AI"
            Write-Log "AI test output: $testResult" "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "❌ AI generation test exception: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Test-FullWorkflow {
    Write-Log "🚀 Testing full Oracle complex query workflow..." "FULL"
    $startTime = Get-Date
    
    try {
        # Run all Oracle tests
        $testResult = & dotnet test SqlTestDataGenerator.Tests --filter "OracleComplexQueryPhuong1989Tests" --logger "console;verbosity=detailed" --verbosity normal 2>&1
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        if ($LASTEXITCODE -eq 0) {
            Write-Log "✅ Full workflow successful in ${duration}s" "FULL"
            return $true
        } else {
            Write-Log "❌ Full workflow failed after ${duration}s" "FULL"
            Write-Log "Full test output: $testResult" "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "❌ Full workflow test exception: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Show-PerformanceSummary {
    param([hashtable]$Results)
    
    Write-Log "📊 Performance Summary" "SUMMARY"
    Write-Log "=====================" "SUMMARY"
    
    $totalTime = 0
    $successCount = 0
    $failureCount = 0
    
    foreach ($test in $Results.Keys) {
        $result = $Results[$test]
        $status = if ($result.Success) { "✅" } else { "❌" }
        $duration = $result.Duration
        $totalTime += $duration
        
        if ($result.Success) { $successCount++ } else { $failureCount++ }
        
        Write-Log "$status $test : ${duration}s" "SUMMARY"
    }
    
    Write-Log "=====================" "SUMMARY"
    Write-Log "Total Time: ${totalTime}s" "SUMMARY"
    Write-Log "Success: $successCount" "SUMMARY"
    Write-Log "Failures: $failureCount" "SUMMARY"
    Write-Log "Log File: $logFile" "SUMMARY"
}

# Main execution
Write-Log "Starting Oracle Complex Query Diagnostic Test" "MAIN"
$results = @{}

# Connection-only mode
if ($TestMode -eq "connection-only") {
    Write-Log "Running connection-only tests..." "MAIN"
    $results["Connection"] = @{
        Success = Test-OracleConnection
        Duration = 0
    }
    $results["SQL Parsing"] = @{
        Success = Test-SqlParsing
        Duration = 0
    }
    $results["Dialect Functions"] = @{
        Success = Test-OracleDialectFunctions
        Duration = 0
    }
    $results["CASE Statement"] = @{
        Success = Test-CaseStatement
        Duration = 0
    }
}

# Quick mode
elseif ($TestMode -eq "quick") {
    Write-Log "Running quick tests..." "MAIN"
    $results["Connection"] = @{
        Success = Test-OracleConnection
        Duration = 0
    }
    $results["Quick Generation"] = @{
        Success = Test-QuickGeneration
        Duration = 0
    }
}

# Full mode
else {
    Write-Log "Running full diagnostic tests..." "MAIN"
    $results["Connection"] = @{
        Success = Test-OracleConnection
        Duration = 0
    }
    $results["SQL Parsing"] = @{
        Success = Test-SqlParsing
        Duration = 0
    }
    $results["Dialect Functions"] = @{
        Success = Test-OracleDialectFunctions
        Duration = 0
    }
    $results["CASE Statement"] = @{
        Success = Test-CaseStatement
        Duration = 0
    }
    $results["Quick Generation"] = @{
        Success = Test-QuickGeneration
        Duration = 0
    }
    
    if ($EnableAI) {
        $results["AI Generation"] = @{
            Success = Test-AIGeneration
            Duration = 0
        }
    }
    
    $results["Full Workflow"] = @{
        Success = Test-FullWorkflow
        Duration = 0
    }
}

# Show summary
Show-PerformanceSummary -Results $results

Write-Log "Diagnostic test completed. Check log file for detailed results: $logFile" "MAIN"

# Return exit code based on results
$hasFailures = $results.Values | Where-Object { -not $_.Success }
if ($hasFailures) {
    Write-Log "❌ Some tests failed. Check the log file for details." "MAIN"
    exit 1
} else {
    Write-Log "✅ All tests passed successfully!" "MAIN"
    exit 0
} 