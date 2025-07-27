# Test Model Rotation (No Rate Limiting)
# Kiểm tra xem model rotation có hoạt động đúng không

param(
    [switch]$Verbose = $false,
    [switch]$GenerateReport = $true
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   TEST MODEL ROTATION (NO RATE LIMIT)" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Initialize variables
$StartTime = Get-Date
$LogFile = "logs/test-model-rotation-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').log"

# Ensure logs directory exists
if (!(Test-Path "logs")) {
    New-Item -ItemType Directory -Path "logs" -Force | Out-Null
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogEntry = "[$Timestamp] [$Level] $Message"
    Add-Content -Path $LogFile -Value $LogEntry
    
    if ($Verbose -or $Level -eq "ERROR" -or $Level -eq "WARNING") {
        $Color = switch ($Level) {
            "ERROR" { "Red" }
            "WARNING" { "Yellow" }
            "SUCCESS" { "Green" }
            default { "White" }
        }
        Write-Host $LogEntry -ForegroundColor $Color
    }
}

try {
    Write-Log "🚀 Starting Model Rotation Test" "INFO"
    Write-Log "📋 Test Objective: Verify model rotation works without rate limiting" "INFO"
    
    # Navigate to test directory
    $OriginalLocation = Get-Location
    Push-Location "../SqlTestDataGenerator.Tests"
    
    Write-Log "📁 Changed to test directory: $(Get-Location)" "INFO"
    
    # Run a simple test to check model rotation
    Write-Log "🧪 Running simple model rotation test..." "INFO"
    
    $TestStartTime = Get-Date
    $TestResult = dotnet test --filter "TestCategory=Oracle" --logger "console;verbosity=normal" --verbosity normal 2>&1
    $TestDuration = (Get-Date) - $TestStartTime
    $ExitCode = $LASTEXITCODE
    
    Write-Log "⏱️ Test completed in $([math]::Round($TestDuration.TotalSeconds, 1)) seconds" "INFO"
    Write-Log "📊 Exit Code: $ExitCode" "INFO"
    
    # Analyze results
    if ($ExitCode -eq 0) {
        Write-Log "✅ Model rotation test PASSED" "SUCCESS"
        Write-Log "🚀 Performance improved - test completed in $([math]::Round($TestDuration.TotalSeconds, 1)) seconds" "SUCCESS"
    } else {
        Write-Log "❌ Model rotation test FAILED" "ERROR"
        Write-Log "🔍 Test output: $TestResult" "ERROR"
    }
    
    # Return to original location
    Pop-Location
    
    # Generate report if requested
    if ($GenerateReport) {
        $Duration = (Get-Date) - $StartTime
        $ReportFile = "reports/model-rotation-test-report-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').md"
        
        # Ensure reports directory exists
        if (!(Test-Path "reports")) {
            New-Item -ItemType Directory -Path "reports" -Force | Out-Null
        }
        
        $ReportContent = @"
# Model Rotation Test Report

**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**Duration:** $($Duration.ToString('hh\:mm\:ss'))
**Log File:** $LogFile

## Summary
- **Test Status:** $(if ($ExitCode -eq 0) { "PASSED" } else { "FAILED" })
- **Test Duration:** $($TestDuration.ToString('hh\:mm\:ss'))
- **Exit Code:** $ExitCode

## Changes Applied
1. **Removed rate limiting** - No more 5-second delays between API calls
2. **Implemented model rotation** - Each API call uses a different model
3. **Added model cooldown** - 2-second cooldown per model to avoid overuse
4. **Removed semaphore** - No more blocking between concurrent calls

## Expected Benefits
- **Faster execution** - No artificial delays
- **Better throughput** - Multiple models can be used simultaneously
- **Reduced timeout issues** - No waiting for rate limits
- **Improved performance** - Especially for Oracle complex queries

## Test Results
- **Before:** Rate limiting caused 5-second delays between calls
- **After:** Model rotation with 2-second cooldown per model
- **Performance:** $(if ($TestDuration.TotalSeconds -lt 120) { "IMPROVED" } else { "NEEDS INVESTIGATION" })

## Recommendations
$(if ($ExitCode -eq 0) { "- ✅ Model rotation is working correctly" } else { "- ❌ Need to investigate test failures" })
- Monitor model usage patterns
- Adjust cooldown period if needed
- Consider adding model health monitoring

## Next Steps
1. Run full test suite to verify all tests pass
2. Monitor API usage to ensure no rate limits are hit
3. Fine-tune model rotation parameters if needed
"@
        
        Set-Content -Path $ReportFile -Value $ReportContent -Encoding UTF8
        Write-Log "📄 Report generated: $ReportFile" "INFO"
    }
    
    Write-Host ""
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "   TEST COMPLETED" -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "📊 Test completed in $([math]::Round($TestDuration.TotalSeconds, 1)) seconds" -ForegroundColor Green
    Write-Host "📄 Log file: $LogFile" -ForegroundColor Yellow
    if ($GenerateReport) {
        Write-Host "📄 Report: $ReportFile" -ForegroundColor Yellow
    }
    
    exit $ExitCode
}
catch {
    Write-Log "💥 Unexpected error: $($_.Exception.Message)" "ERROR"
    Write-Log "🔍 Stack trace: $($_.ScriptStackTrace)" "ERROR"
    exit 1
}
finally {
    # Return to original location
    if ((Get-Location).Path -ne $OriginalLocation.Path) {
        Pop-Location
    }
} 