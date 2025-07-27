# Final Test Script - Complex Test Cases for MySQL and Oracle
# Run comprehensive complex SQL generation tests for both database types

param(
    [string]$DatabaseType = "Both",  # Both, MySQL, Oracle
    [switch]$Verbose = $false,
    [switch]$GenerateReport = $true,
    [switch]$DetailedLogging = $true,
    [switch]$SkipProblematicTests = $false  # Skip known problematic tests
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   FINAL TEST - Complex Test Cases" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Initialize variables
$script:TestResults = @()
$StartTime = Get-Date
$LogFile = "logs/final-test-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').log"
$DetailedLogFile = "logs/final-test-detailed-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').log"

# Ensure logs directory exists
if (!(Test-Path "logs")) {
    New-Item -ItemType Directory -Path "logs" -Force | Out-Null
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogEntry = "[$Timestamp] [$Level] $Message"
    
    # Console output with colors
    switch ($Level) {
        "SUCCESS" { Write-Host $LogEntry -ForegroundColor Green }
        "ERROR" { Write-Host $LogEntry -ForegroundColor Red }
        "WARNING" { Write-Host $LogEntry -ForegroundColor Yellow }
        "DEBUG" { 
            if ($DetailedLogging) { 
                Write-Host $LogEntry -ForegroundColor Gray 
            }
        }
        default { Write-Host $LogEntry }
    }
    
    # Always write to log file
    Add-Content -Path $LogFile -Value $LogEntry
    
    # Write detailed logs if enabled
    if ($DetailedLogging) {
        Add-Content -Path $DetailedLogFile -Value $LogEntry
    }
}

function Write-DetailedLog {
    param([string]$Message, [string]$Context = "DETAILED")
    if ($DetailedLogging) {
        $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
        $LogEntry = "[$Timestamp] [$Context] $Message"
        Add-Content -Path $DetailedLogFile -Value $LogEntry
    }
}

function Run-TestSuite {
    param([string]$TestFilter, [string]$DatabaseName)
    
    Write-Log "🔧 Starting test suite: $DatabaseName" "INFO"
    Write-DetailedLog "Test Filter: $TestFilter"
    Write-DetailedLog "Database Name: $DatabaseName"
    Write-DetailedLog "Current Directory: $(Get-Location)"
    
    $TestStartTime = Get-Date
    Write-DetailedLog "Test Start Time: $TestStartTime"
    
    try {
        # Prepare test command with detailed logging
        $TestCommand = "dotnet test --filter `"$TestFilter`" --verbosity detailed --logger trx --logger console;verbosity=detailed"
        Write-DetailedLog "Executing command: $TestCommand"
        
        # Run the test and capture all output
        Write-Log "⚡ Executing: $DatabaseName tests..." "DEBUG"
        
        $ProcessStartInfo = New-Object System.Diagnostics.ProcessStartInfo
        $ProcessStartInfo.FileName = "dotnet"
        $ProcessStartInfo.Arguments = "test --filter `"$TestFilter`" --verbosity detailed --logger trx --logger console;verbosity=detailed"
        $ProcessStartInfo.RedirectStandardOutput = $true
        $ProcessStartInfo.RedirectStandardError = $true
        $ProcessStartInfo.UseShellExecute = $false
        $ProcessStartInfo.CreateNoWindow = $true
        
        $Process = New-Object System.Diagnostics.Process
        $Process.StartInfo = $ProcessStartInfo
        
        # Capture output streams
        $OutputBuilder = New-Object System.Text.StringBuilder
        $ErrorBuilder = New-Object System.Text.StringBuilder
        
        $OutputEvent = Register-ObjectEvent -InputObject $Process -EventName OutputDataReceived -Action {
            if ($Event.SourceEventArgs.Data) {
                $Event.MessageData.AppendLine($Event.SourceEventArgs.Data)
            }
        } -MessageData $OutputBuilder
        
        $ErrorEvent = Register-ObjectEvent -InputObject $Process -EventName ErrorDataReceived -Action {
            if ($Event.SourceEventArgs.Data) {
                $Event.MessageData.AppendLine($Event.SourceEventArgs.Data)
            }
        } -MessageData $ErrorBuilder
        
        Write-DetailedLog "Starting dotnet test process..."
        $Process.Start()
        $Process.BeginOutputReadLine()
        $Process.BeginErrorReadLine()
        
        # Wait for process with timeout (10 minutes for Oracle Phuong1989 test with AI)
        $TimeoutMs = 600000  # 10 minutes for AI service
        if (!$Process.WaitForExit($TimeoutMs)) {
            Write-Log "⚠️ Test timed out after 10 minutes" "WARNING"
            $Process.Kill()
            throw "Test execution timed out after 10 minutes"
        }
        
        # Clean up events
        Unregister-Event -SourceIdentifier $OutputEvent.Name
        Unregister-Event -SourceIdentifier $ErrorEvent.Name
        
        $TestOutput = $OutputBuilder.ToString()
        $TestError = $ErrorBuilder.ToString()
        $ExitCode = $Process.ExitCode
        
        $TestEndTime = Get-Date
        $TestDuration = $TestEndTime - $TestStartTime
        
        Write-DetailedLog "Test End Time: $TestEndTime"
        Write-DetailedLog "Test Duration: $($TestDuration.ToString('hh\:mm\:ss'))"
        Write-DetailedLog "Exit Code: $ExitCode"
        Write-DetailedLog "Output Length: $($TestOutput.Length) characters"
        Write-DetailedLog "Error Length: $($TestError.Length) characters"
        
        # Log full output for debugging
        if ($TestOutput) {
            Write-DetailedLog "=== FULL TEST OUTPUT START ==="
            Write-DetailedLog $TestOutput
            Write-DetailedLog "=== FULL TEST OUTPUT END ==="
        }
        
        if ($TestError) {
            Write-DetailedLog "=== FULL TEST ERROR START ==="
            Write-DetailedLog $TestError
            Write-DetailedLog "=== FULL TEST ERROR END ==="
        }
        
        # Analyze test results - Fixed regex patterns for .NET test output
        $PassedCount = 0
        $FailedCount = 0
        $SkippedCount = 0
        
        # .NET test output format: "Failed:     0, Passed:    10, Skipped:     0, Total:    10"
        if ($TestOutput -match "Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+)") { 
            $FailedCount = [int]$matches[1]
            $PassedCount = [int]$matches[2] 
            $SkippedCount = [int]$matches[3]
        }
        # Fallback patterns for other formats
        elseif ($TestOutput -match "Passed:\s*(\d+)") { $PassedCount = [int]$matches[1] }
        if ($TestOutput -match "Failed:\s*(\d+)" -and $FailedCount -eq 0) { $FailedCount = [int]$matches[1] }
        if ($TestOutput -match "Skipped:\s*(\d+)" -and $SkippedCount -eq 0) { $SkippedCount = [int]$matches[1] }
        
        Write-DetailedLog "Test Results Analysis - Passed: $PassedCount, Failed: $FailedCount, Skipped: $SkippedCount"
        
        if ($ExitCode -eq 0) {
            Write-Log "✅ $DatabaseName tests PASSED ($PassedCount passed, $SkippedCount skipped)" "SUCCESS"
            $script:TestResults += @{
                Database = $DatabaseName
                TestFilter = $TestFilter
                Status = "PASSED"
                Duration = $TestDuration.ToString('hh\:mm\:ss')
                PassedCount = $PassedCount
                FailedCount = $FailedCount
                SkippedCount = $SkippedCount
                Output = $TestOutput
                Error = $TestError
                ExitCode = $ExitCode
            }
            return $true
        } else {
            Write-Log "❌ $DatabaseName tests FAILED ($PassedCount passed, $FailedCount failed, $SkippedCount skipped)" "ERROR"
            
            # Extract specific failure information
            $FailureDetails = ""
            if ($TestOutput -match "Failed!\s*-\s*Failed:\s*\d+") {
                $FailureLines = $TestOutput -split "`n" | Where-Object { $_ -match "(Failed|Error|Exception)" }
                $FailureDetails = $FailureLines -join "`n"
                Write-Log "🔍 Failure details: $($FailureLines[0])" "ERROR"
            }
            
            $script:TestResults += @{
                Database = $DatabaseName
                TestFilter = $TestFilter
                Status = "FAILED"
                Duration = $TestDuration.ToString('hh\:mm\:ss')
                PassedCount = $PassedCount
                FailedCount = $FailedCount
                SkippedCount = $SkippedCount
                Output = $TestOutput
                Error = $TestError
                ExitCode = $ExitCode
                FailureDetails = $FailureDetails
            }
            return $false
        }
    }
    catch {
        $TestEndTime = Get-Date
        $TestDuration = $TestEndTime - $TestStartTime
        
        Write-Log "💥 Exception in $DatabaseName tests: $($_.Exception.Message)" "ERROR"
        Write-DetailedLog "Exception Details: $($_.Exception.ToString())"
        Write-DetailedLog "Stack Trace: $($_.Exception.StackTrace)"
        
        $script:TestResults += @{
            Database = $DatabaseName
            TestFilter = $TestFilter
            Status = "EXCEPTION"
            Duration = $TestDuration.ToString('hh\:mm\:ss')
            PassedCount = 0
            FailedCount = 0
            SkippedCount = 0
            Output = ""
            Error = $_.Exception.Message
            ExitCode = -1
            Exception = $_.Exception.ToString()
        }
        return $false
    }
}

# Navigate to test directory from scripts folder
$OriginalLocation = Get-Location
Write-DetailedLog "Original Location: $OriginalLocation"

Push-Location "SqlTestDataGenerator.Tests"
$TestLocation = Get-Location
Write-DetailedLog "Test Directory: $TestLocation"

try {
    Write-Log "🚀 Starting Final Test Suite execution" "INFO"
    Write-Log "📋 Database Type: $DatabaseType" "INFO"
    Write-Log "📁 Test Directory: $TestLocation" "INFO"
    Write-Log "📄 Log File: $LogFile" "INFO"
    if ($DetailedLogging) {
        Write-Log "📄 Detailed Log File: $DetailedLogFile" "INFO"
    }
    
    # Check if project files exist
    $ProjectFile = "SqlTestDataGenerator.Tests.csproj"
    if (!(Test-Path $ProjectFile)) {
        Write-Log "❌ Project file not found: $ProjectFile" "ERROR"
        throw "Project file not found in current directory"
    }
    Write-DetailedLog "Project file found: $ProjectFile"
    
    # Build the project first
    Write-Log "🔨 Building test project..." "INFO"
    Write-DetailedLog "Running: dotnet build --verbosity normal"
    
    $BuildStartTime = Get-Date
    $BuildResult = dotnet build --verbosity normal 2>&1
    $BuildEndTime = Get-Date
    $BuildDuration = $BuildEndTime - $BuildStartTime
    
    Write-DetailedLog "Build Duration: $($BuildDuration.ToString('hh\:mm\:ss'))"
    Write-DetailedLog "Build Output: $BuildResult"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Log "❌ Build failed with exit code: $LASTEXITCODE" "ERROR"
        Write-DetailedLog "Build failure details: $BuildResult"
        exit 1
    }
    Write-Log "✅ Build successful" "SUCCESS"
    
    $AllTestsPassed = $true
    $TotalTestCount = 0
    $TestsActuallyRun = 0  # Đếm số test suite thực sự chạy
    
    # Run MySQL Complex Tests (Top Priority)
    if ($DatabaseType -eq "Both" -or $DatabaseType -eq "MySQL") {
        Write-Log "🐬 === MYSQL COMPLEX SQL TESTS (Top 5) ===" "INFO"
        
        $MySQLTests = @(
            @{Filter = "ComplexSqlGenerationTests"; Name = "MySQL Complex SQL Generation"},
            @{Filter = "MySQLDateFunctionConverterTests"; Name = "MySQL Date Function Converter"},
            @{Filter = "MySQLIntegrationDuplicateKeyTests"; Name = "MySQL Duplicate Key Handling"},
            @{Filter = "RealMySQLIntegrationTests"; Name = "MySQL Real Integration"},
            @{Filter = "SqlQueryParserV3Tests"; Name = "MySQL SQL Parser V3"}
        )
        
        $TotalTestCount += $MySQLTests.Count
        Write-DetailedLog "MySQL Complex Test Count: $($MySQLTests.Count)"
        
        # Kiểm tra xem có test nào tồn tại không
        Write-Log "🔍 Checking if MySQL test classes exist..." "DEBUG"
        $TestDiscoveryCommand = "dotnet test --list-tests --filter `"ComplexSqlGenerationTests|MySQLDateFunctionConverterTests|MySQLIntegrationDuplicateKeyTests|RealMySQLIntegrationTests|SqlQueryParserV3Tests`" 2>&1"
        $TestDiscoveryResult = Invoke-Expression $TestDiscoveryCommand
        Write-DetailedLog "Test Discovery Result: $TestDiscoveryResult"
        
        if ($LASTEXITCODE -ne 0 -or $TestDiscoveryResult -match "No test is available") {
            Write-Log "⚠️ Warning: Some MySQL test classes may not exist or be discoverable" "WARNING"
        }
        
        foreach ($Test in $MySQLTests) {
            Write-Log "🔄 Running: $($Test.Name)" "INFO"
            Write-DetailedLog "MySQL Test Filter: $($Test.Filter)"
            
            $TestPassed = Run-TestSuite -TestFilter $Test.Filter -DatabaseName $Test.Name
            $TestsActuallyRun++  # Tăng số test suite đã chạy
            if (!$TestPassed) { 
                $AllTestsPassed = $false 
                Write-Log "⚠️ MySQL test failed: $($Test.Name)" "WARNING"
            }
            
            Write-DetailedLog "Waiting 2 seconds before next test..."
            Start-Sleep -Seconds 2
        }
    }
    
    # Run Oracle Complex Tests (Phuong1989 Only)
    if ($DatabaseType -eq "Both" -or $DatabaseType -eq "Oracle") {
        Write-Log "🔶 === ORACLE COMPLEX SQL TESTS (Phuong1989 Only) ===" "INFO"
        
        $OracleTests = @(
            @{Filter = "OracleComplexQueryPhuong1989Tests&TestCategory!=AI-Service"; Name = "Oracle Complex Query Phuong1989 (Enhanced AI with Rate Limit Handling)"}
        )
        
        $TotalTestCount += $OracleTests.Count
        Write-DetailedLog "Oracle Complex Test Count: $($OracleTests.Count)"
        
        # Kiểm tra xem có test nào tồn tại không
        Write-Log "🔍 Checking if Oracle test classes exist..." "DEBUG"
        $TestDiscoveryCommand = "dotnet test --list-tests --filter `"OracleComplexQueryPhuong1989Tests`" 2>&1"
        $TestDiscoveryResult = Invoke-Expression $TestDiscoveryCommand
        Write-DetailedLog "Test Discovery Result: $TestDiscoveryResult"
        
        if ($LASTEXITCODE -ne 0 -or $TestDiscoveryResult -match "No test is available") {
            Write-Log "⚠️ Warning: Oracle test class may not exist or be discoverable" "WARNING"
        }
        
        foreach ($Test in $OracleTests) {
            Write-Log "🔄 Running: $($Test.Name)" "INFO"
            Write-DetailedLog "Oracle Test Filter: $($Test.Filter)"
            
            $TestPassed = Run-TestSuite -TestFilter $Test.Filter -DatabaseName $Test.Name
            $TestsActuallyRun++  # Tăng số test suite đã chạy
            if (!$TestPassed) { 
                $AllTestsPassed = $false 
                Write-Log "⚠️ Oracle test failed: $($Test.Name)" "WARNING"
            }
            
            Write-DetailedLog "Waiting 2 seconds before next test..."
            Start-Sleep -Seconds 2
        }
    }
    
}
finally {
    Pop-Location
    Write-DetailedLog "Returned to original location: $(Get-Location)"
}

# Generate Summary Report
$EndTime = Get-Date
$Duration = $EndTime - $StartTime

Write-Log "📊 === FINAL TEST SUMMARY ===" "INFO"
Write-Log "⏱️ Total Duration: $($Duration.ToString('hh\:mm\:ss'))" "INFO"
Write-Log "🔢 Total Test Suites: $($script:TestResults.Count)" "INFO"
Write-Log "🎯 Expected Test Suites: $TotalTestCount" "INFO"
Write-Log "🏃 Test Suites Actually Run: $TestsActuallyRun" "INFO"

$PassedTests = ($script:TestResults | Where-Object { $_.Status -eq "PASSED" }).Count
$FailedTests = ($script:TestResults | Where-Object { $_.Status -eq "FAILED" }).Count
$ExceptionTests = ($script:TestResults | Where-Object { $_.Status -eq "EXCEPTION" }).Count

$TotalPassed = ($script:TestResults | ForEach-Object { $_.PassedCount } | Measure-Object -Sum).Sum
$TotalFailed = ($script:TestResults | ForEach-Object { $_.FailedCount } | Measure-Object -Sum).Sum
$TotalSkipped = ($script:TestResults | ForEach-Object { $_.SkippedCount } | Measure-Object -Sum).Sum

Write-Log "✅ Test Suites Passed: $PassedTests" "SUCCESS"
Write-Log "❌ Test Suites Failed: $FailedTests" "ERROR"
Write-Log "💥 Test Suites with Exceptions: $ExceptionTests" "ERROR"
Write-Log "📈 Individual Tests - Passed: $TotalPassed, Failed: $TotalFailed, Skipped: $TotalSkipped" "INFO"

# Show failed tests details
if ($FailedTests -gt 0 -or $ExceptionTests -gt 0) {
    Write-Log "🔍 === FAILURE ANALYSIS ===" "ERROR"
    foreach ($Result in ($script:TestResults | Where-Object { $_.Status -ne "PASSED" })) {
        Write-Log "❌ $($Result.Database): $($Result.Status)" "ERROR"
        Write-Log "   📊 Individual: $($Result.PassedCount) passed, $($Result.FailedCount) failed, $($Result.SkippedCount) skipped" "ERROR"
        
        if ($Result.FailureDetails) {
            $FailureLines = $Result.FailureDetails.Split("`n") | Where-Object { $_.Trim() -ne "" } | Select-Object -First 3
            foreach ($Line in $FailureLines) {
                Write-Log "   📋 $($Line.Trim())" "ERROR"
            }
        }
        
        if ($Result.Exception) {
            Write-Log "   💥 Exception: $($Result.Exception.Split("`n")[0])" "ERROR"
        }
        
        if ($Result.Error -and $Result.Error -ne $Result.Exception) {
            Write-Log "   🔥 Error: $($Result.Error.Split("`n")[0])" "ERROR"
        }
        Write-Log "" # Empty line for better readability
    }
}

# Generate detailed report if requested
if ($GenerateReport) {
    $ReportFile = "reports/final-test-report-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').md"
    
    # Ensure reports directory exists
    if (!(Test-Path "reports")) {
        New-Item -ItemType Directory -Path "reports" -Force | Out-Null
    }
    
    $ReportContent = @"
# Final Test Report - Complex Test Cases

**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**Duration:** $($Duration.ToString('hh\:mm\:ss'))
**Database Type:** $DatabaseType
**Log File:** $LogFile
**Detailed Log:** $DetailedLogFile

## Summary
- **Total Test Suites:** $($script:TestResults.Count) / $TotalTestCount expected
- **Suites Passed:** $PassedTests ✅
- **Suites Failed:** $FailedTests ❌  
- **Suites with Exceptions:** $ExceptionTests 💥
- **Individual Tests:** $TotalPassed passed, $TotalFailed failed, $TotalSkipped skipped

## Test Results Details

"@

    foreach ($Result in $script:TestResults) {
        $StatusIcon = switch ($Result.Status) {
            "PASSED" { "✅ [PASS]" }
            "FAILED" { "❌ [FAIL]" }
            "EXCEPTION" { "💥 [ERROR]" }
        }
        
        $ReportContent += @"

### $($Result.Database) $StatusIcon

- **Status:** $($Result.Status)
- **Filter:** $($Result.TestFilter)
- **Duration:** $($Result.Duration)
- **Individual Results:** $($Result.PassedCount) passed, $($Result.FailedCount) failed, $($Result.SkippedCount) skipped
- **Exit Code:** $($Result.ExitCode)

"@
        
        if ($Result.Status -eq "FAILED" -and $Result.FailureDetails) {
            $ReportContent += @"

**Failure Details:**
```
$($Result.FailureDetails)
```

"@
        }
        
        if ($Result.Status -eq "EXCEPTION") {
            $ReportContent += @"

**Exception:**
```
$($Result.Error)
```

"@
        }
        
        if (($Result.Status -ne "PASSED") -and $Verbose -and $Result.Output) {
            $ReportContent += @"

**Full Output:**
```
$($Result.Output)
```

"@
        }
    }
    
    $ReportContent += @"

## Diagnostics Information

### Log Files
- **Main Log:** `$LogFile`
- **Detailed Log:** `$DetailedLogFile`

### Environment
- **Test Directory:** $TestLocation
- **PowerShell Version:** $($PSVersionTable.PSVersion)
- **Operating System:** $($PSVersionTable.OS)

## Recommendations

"@

    if ($FailedTests -gt 0 -or $ExceptionTests -gt 0) {
        $ReportContent += @"
### Issues Found ❌
- **Action Required:** Review failed test outputs above
- **Check:** Database connectivity (MySQL/Oracle)
- **Verify:** Test data setup and schema
- **Validate:** API keys and configuration
- **Review:** Detailed logs in `$DetailedLogFile`

### Debugging Steps
1. Check the detailed log file for specific error messages
2. Verify database connections are working
3. Ensure all required services are running
4. Check for missing dependencies or configuration
5. Run individual failed tests with `dotnet test --filter "TestName"`

"@
    } else {
        $ReportContent += @"
### All Tests Passed ✅
- Complex SQL generation working correctly for both databases
- Integration tests successful
- System ready for production use
- All $TotalPassed individual tests passed

"@
    }
    
    $ReportContent += @"

### Next Steps
1. Address any failed tests before deployment
2. Review performance metrics if available
3. Update documentation based on test results
4. Consider adding additional edge case tests
5. Archive test logs for future reference

---
*Generated by final-test.ps1 with detailed logging*
*Detailed logs available at: $DetailedLogFile*
"@

    Set-Content -Path $ReportFile -Value $ReportContent
    Write-Log "📋 Report generated: $ReportFile" "INFO"
}

# Final result
Write-Log "🏁 === FINAL RESULT ===" "INFO"

# Kiểm tra xem có test nào thực sự chạy không
$TotalExecutedTests = $TotalPassed + $TotalFailed + $TotalSkipped
Write-DetailedLog "Total Executed Tests: $TotalExecutedTests"
Write-DetailedLog "AllTestsPassed Flag: $AllTestsPassed"
Write-DetailedLog "Test Results Count: $($script:TestResults.Count)"
Write-DetailedLog "Tests Actually Run: $TestsActuallyRun"
Write-DetailedLog "Expected Test Count: $TotalTestCount"

# Nếu không có test nào chạy, báo lỗi
if ($TotalExecutedTests -eq 0 -or $TestsActuallyRun -eq 0) {
    Write-Host ""
    Write-Host "⚠️ [ERROR] NO TESTS WERE EXECUTED!" -ForegroundColor Yellow
    Write-Host "📊 Results: 0 tests executed (0 passed, 0 failed, 0 skipped)" -ForegroundColor Yellow
    Write-Host "🔍 Possible causes:" -ForegroundColor Yellow
    Write-Host "   - Test filters don't match any tests in the project" -ForegroundColor Yellow
    Write-Host "   - Project build failed" -ForegroundColor Yellow
    Write-Host "   - No test classes found" -ForegroundColor Yellow
    Write-Host "   - Database connection issues" -ForegroundColor Yellow
    Write-Host "📋 Check detailed logs: $DetailedLogFile" -ForegroundColor Yellow
    Write-Host "📋 Check report: Generated in reports/ folder" -ForegroundColor Yellow
    Write-Host ""
    exit 2
}
# Nếu có test chạy nhưng tất cả đều pass
elseif ($AllTestsPassed -and $TotalFailed -eq 0) {
    Write-Host ""
    Write-Host "🎉 [SUCCESS] ALL TESTS PASSED! System ready for deployment." -ForegroundColor Green
    Write-Host "📊 Results: $TotalPassed tests passed, $TotalSkipped skipped" -ForegroundColor Green
    Write-Host "📋 Report: Check the generated report for details" -ForegroundColor Green
    Write-Host ""
    exit 0
} 
# Nếu có test fail hoặc exception
else {
    Write-Host ""
    Write-Host "💥 [FAILED] SOME TESTS FAILED! Review the results above." -ForegroundColor Red
    Write-Host "📊 Results: $TotalPassed passed, $TotalFailed failed, $TotalSkipped skipped" -ForegroundColor Red
    Write-Host "📋 Check detailed logs: $DetailedLogFile" -ForegroundColor Red
    Write-Host "📋 Check report: Generated in reports/ folder" -ForegroundColor Red
    Write-Host ""
    exit 1
} 