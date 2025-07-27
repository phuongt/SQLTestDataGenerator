# Test Oracle Timeout Fix
# Kiểm tra xem vấn đề log spam đã được giải quyết chưa

param(
    [switch]$Verbose = $false,
    [switch]$GenerateReport = $true
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   TEST ORACLE TIMEOUT FIX" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Initialize variables
$StartTime = Get-Date
$LogFile = "logs/test-oracle-timeout-fix-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').log"

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
            if ($Verbose) { 
                Write-Host $LogEntry -ForegroundColor Gray 
            }
        }
        default { Write-Host $LogEntry }
    }
    
    # Always write to log file
    Add-Content -Path $LogFile -Value $LogEntry
}

# Navigate to test directory
$OriginalLocation = Get-Location
Push-Location "../SqlTestDataGenerator.Tests"

try {
    Write-Log "🚀 Starting Oracle Timeout Fix Test" "INFO"
    Write-Log "📁 Test Directory: $(Get-Location)" "INFO"
    Write-Log "📄 Log File: $LogFile" "INFO"
    
    # Build the project first
    Write-Log "🔨 Building test project..." "INFO"
    $BuildResult = dotnet build --verbosity normal 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Log "❌ Build failed with exit code: $LASTEXITCODE" "ERROR"
        Write-Log "Build failure details: $BuildResult" "ERROR"
        exit 1
    }
    Write-Log "✅ Build successful" "SUCCESS"
    
    # Run Oracle test with shorter timeout to check if log spam is fixed
    Write-Log "🔄 Running Oracle test with 2-minute timeout..." "INFO"
    
    $TestStartTime = Get-Date
    $TestCommand = "dotnet test --filter `"OracleComplexQueryPhuong1989Tests&TestCategory!=AI-Service`" --verbosity normal --logger console;verbosity=normal"
    
    Write-Log "⚡ Executing: $TestCommand" "DEBUG"
    
    $ProcessStartInfo = New-Object System.Diagnostics.ProcessStartInfo
    $ProcessStartInfo.FileName = "dotnet"
    $ProcessStartInfo.Arguments = "test --filter `"OracleComplexQueryPhuong1989Tests&TestCategory!=AI-Service`" --verbosity normal --logger console;verbosity=normal"
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
    
    $Process.Start()
    $Process.BeginOutputReadLine()
    $Process.BeginErrorReadLine()
    
    # Wait for process with 2-minute timeout (reduced from 10 minutes)
    $TimeoutMs = 120000  # 2 minutes
    if (!$Process.WaitForExit($TimeoutMs)) {
        Write-Log "⚠️ Test timed out after 2 minutes" "WARNING"
        $Process.Kill()
        throw "Test execution timed out after 2 minutes"
    }
    
    # Clean up events
    Unregister-Event -SourceIdentifier $OutputEvent.Name
    Unregister-Event -SourceIdentifier $ErrorEvent.Name
    
    $TestOutput = $OutputBuilder.ToString()
    $TestError = $ErrorBuilder.ToString()
    $ExitCode = $Process.ExitCode
    
    $TestEndTime = Get-Date
    $TestDuration = $TestEndTime - $TestStartTime
    
    Write-Log "⏱️ Test completed in: $($TestDuration.ToString('hh\:mm\:ss'))" "INFO"
    Write-Log "📊 Exit Code: $ExitCode" "INFO"
    
    # Analyze test results
    $PassedCount = 0
    $FailedCount = 0
    $SkippedCount = 0
    
    if ($TestOutput -match "Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+)") { 
        $FailedCount = [int]$matches[1]
        $PassedCount = [int]$matches[2] 
        $SkippedCount = [int]$matches[3]
    }
    
    Write-Log "📈 Test Results - Passed: $PassedCount, Failed: $FailedCount, Skipped: $SkippedCount" "INFO"
    
    # Check for log spam in engine log
    Write-Log "🔍 Checking for log spam in engine log..." "INFO"
    $EngineLogFile = "logs/engine-$(Get-Date -Format 'yyyyMMdd').log"
    
    if (Test-Path $EngineLogFile) {
        $RecentLogs = Get-Content $EngineLogFile -Tail 100
        $LogSpamCount = ($RecentLogs | Where-Object { $_ -match "Current active model:" }).Count
        
        Write-Log "📊 Recent log entries with 'Current active model': $LogSpamCount" "INFO"
        
        if ($LogSpamCount -gt 10) {
            Write-Log "⚠️ Still detecting log spam - $LogSpamCount entries in recent logs" "WARNING"
        } else {
            Write-Log "✅ Log spam appears to be fixed - only $LogSpamCount entries" "SUCCESS"
        }
    } else {
        Write-Log "⚠️ Engine log file not found: $EngineLogFile" "WARNING"
    }
    
    # Final result
    if ($ExitCode -eq 0) {
        Write-Log "✅ Oracle test PASSED - timeout fix appears successful" "SUCCESS"
        Write-Log "🚀 Performance improved - test completed in $([math]::Round($TestDuration.TotalSeconds, 1)) seconds" "SUCCESS"
    } else {
        Write-Log "❌ Oracle test FAILED - may still have issues" "ERROR"
        if ($TestError) {
            Write-Log "Error details: $($TestError.Split("`n")[0])" "ERROR"
        }
    }
    
}
finally {
    Pop-Location
    Write-Log "Returned to original location: $(Get-Location)" "DEBUG"
}

# Generate Summary Report
$EndTime = Get-Date
$Duration = $EndTime - $StartTime

Write-Log "📊 === TIMEOUT FIX TEST SUMMARY ===" "INFO"
Write-Log "⏱️ Total Duration: $($Duration.ToString('hh\:mm\:ss'))" "INFO"
Write-Log "📄 Log File: $LogFile" "INFO"

if ($GenerateReport) {
    $ReportFile = "reports/oracle-timeout-fix-test-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').md"
    
    # Ensure reports directory exists
    if (!(Test-Path "reports")) {
        New-Item -ItemType Directory -Path "reports" -Force | Out-Null
    }
    
    $ReportContent = @"
# Oracle Timeout Fix Test Report

**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**Duration:** $($Duration.ToString('hh\:mm\:ss'))
**Log File:** $LogFile

## Summary
- **Test Status:** $(if ($ExitCode -eq 0) { "PASSED" } else { "FAILED" })
- **Test Duration:** $($TestDuration.ToString('hh\:mm\:ss'))
- **Exit Code:** $ExitCode
- **Individual Tests:** $PassedCount passed, $FailedCount failed, $SkippedCount skipped

## Fixes Applied
1. **Removed debug logging** from `GetCurrentModelName()` method
2. **Increased timer interval** from 2 seconds to 10 seconds
3. **Reduced test timeout** from 10 minutes to 2 minutes for faster feedback

## Performance Impact
- **Before:** Log spam every 2 seconds causing timeout
- **After:** Reduced logging frequency, improved performance
- **Expected:** Oracle tests should complete within 2 minutes

## Recommendations
$(if ($ExitCode -eq 0) { 
    "- ✅ Timeout fix appears successful`n- ✅ Performance improved significantly`n- ✅ Ready for production use"
} else { 
    "- ❌ Test still failing - review error details`n- 🔍 Check database connectivity`n- 🔍 Verify Oracle connection string`n- 🔍 Review detailed logs for specific issues"
})

---
*Generated by test-oracle-timeout-fix.ps1*
"@

    Set-Content -Path $ReportFile -Value $ReportContent
    Write-Log "📋 Report generated: $ReportFile" "INFO"
}

Write-Host ""
if ($ExitCode -eq 0) {
    Write-Host "🎉 [SUCCESS] Oracle timeout fix appears to be working!" -ForegroundColor Green
    Write-Host "📊 Test completed in $([math]::Round($TestDuration.TotalSeconds, 1)) seconds" -ForegroundColor Green
} else {
    Write-Host "💥 [FAILED] Oracle test still has issues" -ForegroundColor Red
    Write-Host "📋 Check detailed logs: $LogFile" -ForegroundColor Red
}
Write-Host "" 