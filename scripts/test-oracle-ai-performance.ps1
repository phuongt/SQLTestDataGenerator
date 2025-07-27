# Oracle AI Performance Test Script
# Test AI service performance với Oracle complex queries

param(
    [switch]$Verbose = $false,
    [switch]$GenerateReport = $true,
    [switch]$DetailedLogging = $true
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   ORACLE AI PERFORMANCE TEST" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Initialize variables
$script:TestResults = @()
$StartTime = Get-Date
$LogFile = "logs/oracle-ai-test-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').log"
$DetailedLogFile = "logs/oracle-ai-test-detailed-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').log"

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

function Run-AITestSuite {
    param([string]$TestFilter, [string]$TestName)
    
    Write-Log "🤖 Starting AI test suite: $TestName" "INFO"
    Write-DetailedLog "AI Test Filter: $TestFilter"
    Write-DetailedLog "Test Name: $TestName"
    
    $TestStartTime = Get-Date
    Write-DetailedLog "AI Test Start Time: $TestStartTime"
    
    try {
        # Run AI test with extended timeout
        $TestCommand = "dotnet test --filter `"$TestFilter`" --verbosity detailed --logger trx --logger console;verbosity=detailed"
        Write-DetailedLog "Executing AI command: $TestCommand"
        
        Write-Log "🚀 Executing AI test: $TestName..." "DEBUG"
        
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
        
        Write-DetailedLog "Starting AI test process..."
        $Process.Start()
        $Process.BeginOutputReadLine()
        $Process.BeginErrorReadLine()
        
        # Wait for process with extended timeout (10 minutes for AI tests)
        $TimeoutMs = 600000  # 10 minutes
        if (!$Process.WaitForExit($TimeoutMs)) {
            Write-Log "⚠️ AI test timed out after 10 minutes" "WARNING"
            $Process.Kill()
            throw "AI test execution timed out after 10 minutes"
        }
        
        # Clean up events
        Unregister-Event -SourceIdentifier $OutputEvent.Name
        Unregister-Event -SourceIdentifier $ErrorEvent.Name
        
        $TestOutput = $OutputBuilder.ToString()
        $TestError = $ErrorBuilder.ToString()
        $ExitCode = $Process.ExitCode
        
        $TestEndTime = Get-Date
        $TestDuration = $TestEndTime - $TestStartTime
        
        Write-DetailedLog "AI Test End Time: $TestEndTime"
        Write-DetailedLog "AI Test Duration: $($TestDuration.ToString('hh\:mm\:ss'))"
        Write-DetailedLog "Exit Code: $ExitCode"
        
        # Analyze test results
        $PassedCount = 0
        $FailedCount = 0
        $SkippedCount = 0
        
        if ($TestOutput -match "Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+)") { 
            $FailedCount = [int]$matches[1]
            $PassedCount = [int]$matches[2] 
            $SkippedCount = [int]$matches[3]
        }
        
        Write-DetailedLog "AI Test Results Analysis - Passed: $PassedCount, Failed: $FailedCount, Skipped: $SkippedCount"
        
        if ($ExitCode -eq 0) {
            Write-Log "✅ AI test PASSED ($PassedCount passed, $SkippedCount skipped) in $($TestDuration.ToString('mm\:ss'))" "SUCCESS"
            $script:TestResults += @{
                TestName = $TestName
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
            Write-Log "❌ AI test FAILED ($PassedCount passed, $FailedCount failed, $SkippedCount skipped) in $($TestDuration.ToString('mm\:ss'))" "ERROR"
            
            $script:TestResults += @{
                TestName = $TestName
                TestFilter = $TestFilter
                Status = "FAILED"
                Duration = $TestDuration.ToString('hh\:mm\:ss')
                PassedCount = $PassedCount
                FailedCount = $FailedCount
                SkippedCount = $SkippedCount
                Output = $TestOutput
                Error = $TestError
                ExitCode = $ExitCode
            }
            return $false
        }
    }
    catch {
        $TestEndTime = Get-Date
        $TestDuration = $TestEndTime - $TestStartTime
        
        Write-Log "💥 Exception in AI test: $($_.Exception.Message)" "ERROR"
        Write-DetailedLog "AI Exception Details: $($_.Exception.ToString())"
        
        $script:TestResults += @{
            TestName = $TestName
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

# Navigate to test directory
$OriginalLocation = Get-Location
Write-DetailedLog "Original Location: $OriginalLocation"

Push-Location "SqlTestDataGenerator.Tests"
$TestLocation = Get-Location
Write-DetailedLog "Test Directory: $TestLocation"

try {
    Write-Log "🚀 Starting Oracle AI Performance Test Suite" "INFO"
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
    $BuildResult = dotnet build --verbosity normal 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Log "❌ Build failed with exit code: $LASTEXITCODE" "ERROR"
        exit 1
    }
    Write-Log "✅ Build successful" "SUCCESS"
    
    $AllTestsPassed = $true
    
    # Run Oracle AI Tests
    Write-Log "🤖 === ORACLE AI PERFORMANCE TESTS ===" "INFO"
    
    $OracleAITests = @(
        @{Filter = "TestCategory=AI-Service"; Name = "Oracle AI Service Performance Test"}
    )
    
    foreach ($Test in $OracleAITests) {
        Write-Log "🔄 Running: $($Test.Name)" "INFO"
        Write-DetailedLog "Oracle AI Test Filter: $($Test.Filter)"
        
        $TestPassed = Run-AITestSuite -TestFilter $Test.Filter -TestName $Test.Name
        if (!$TestPassed) { 
            $AllTestsPassed = $false 
            Write-Log "⚠️ Oracle AI test failed: $($Test.Name)" "WARNING"
        }
        
        Write-DetailedLog "Waiting 5 seconds before next AI test..."
        Start-Sleep -Seconds 5
    }
    
}
finally {
    Pop-Location
    Write-DetailedLog "Returned to original location: $(Get-Location)"
}

# Generate Summary Report
$EndTime = Get-Date
$Duration = $EndTime - $StartTime

Write-Log "📊 === ORACLE AI PERFORMANCE TEST SUMMARY ===" "INFO"
Write-Log "⏱️ Total Duration: $($Duration.ToString('hh\:mm\:ss'))" "INFO"
Write-Log "🔢 Total AI Test Suites: $($script:TestResults.Count)" "INFO"

$PassedTests = ($script:TestResults | Where-Object { $_.Status -eq "PASSED" }).Count
$FailedTests = ($script:TestResults | Where-Object { $_.Status -eq "FAILED" }).Count
$ExceptionTests = ($script:TestResults | Where-Object { $_.Status -eq "EXCEPTION" }).Count

$TotalPassed = ($script:TestResults | ForEach-Object { $_.PassedCount } | Measure-Object -Sum).Sum
$TotalFailed = ($script:TestResults | ForEach-Object { $_.FailedCount } | Measure-Object -Sum).Sum
$TotalSkipped = ($script:TestResults | ForEach-Object { $_.SkippedCount } | Measure-Object -Sum).Sum

Write-Log "✅ AI Test Suites Passed: $PassedTests" "SUCCESS"
Write-Log "❌ AI Test Suites Failed: $FailedTests" "ERROR"
Write-Log "💥 AI Test Suites with Exceptions: $ExceptionTests" "ERROR"
Write-Log "📈 Individual AI Tests - Passed: $TotalPassed, Failed: $TotalFailed, Skipped: $TotalSkipped" "INFO"

# Show performance analysis
Write-Log "🚀 === AI PERFORMANCE ANALYSIS ===" "INFO"
foreach ($Result in $script:TestResults) {
    Write-Log "📊 $($Result.TestName): $($Result.Status) in $($Result.Duration)" "INFO"
    
    if ($Result.Status -eq "PASSED") {
        $durationSeconds = [TimeSpan]::Parse($Result.Duration).TotalSeconds
        if ($durationSeconds < 60) {
            Write-Log "   ⚡ Fast AI performance: < 1 minute" "SUCCESS"
        } elseif ($durationSeconds < 300) {
            Write-Log "   🐌 Moderate AI performance: 1-5 minutes" "WARNING"
        } else {
            Write-Log "   🐌 Slow AI performance: > 5 minutes" "ERROR"
        }
    }
}

# Final result
Write-Log "🏁 === FINAL AI TEST RESULT ===" "INFO"
if ($AllTestsPassed) {
    Write-Host ""
    Write-Host "🎉 [SUCCESS] ALL ORACLE AI TESTS PASSED!" -ForegroundColor Green
    Write-Host "📊 Results: $TotalPassed AI tests passed, $TotalSkipped skipped" -ForegroundColor Green
    Write-Host "📋 Check detailed logs: $DetailedLogFile" -ForegroundColor Green
    Write-Host ""
    exit 0
} else {
    Write-Host ""
    Write-Host "💥 [FAILED] SOME ORACLE AI TESTS FAILED!" -ForegroundColor Red
    Write-Host "📊 Results: $TotalPassed passed, $TotalFailed failed, $TotalSkipped skipped" -ForegroundColor Red
    Write-Host "📋 Check detailed logs: $DetailedLogFile" -ForegroundColor Red
    Write-Host ""
    exit 1
} 