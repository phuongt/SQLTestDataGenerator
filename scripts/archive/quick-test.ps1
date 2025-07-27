# =====================================================================
# Quick Test Script - Fast TC001 Execution
# =====================================================================
# PURPOSE: Quick run of TC001 test method without verbose logging
# USAGE: .\scripts\quick-test.ps1
# =====================================================================

param(
    [switch]$Quiet = $false
)

# Colors for output
$script:Green = [System.ConsoleColor]::Green
$script:Red = [System.ConsoleColor]::Red
$script:Yellow = [System.ConsoleColor]::Yellow
$script:Cyan = [System.ConsoleColor]::Cyan

function Write-ColoredOutput {
    param(
        [string]$Message, 
        [System.ConsoleColor]$Color = [System.ConsoleColor]::White
    )
    Write-Host $Message -ForegroundColor $Color
}

if (-not $Quiet) {
    Write-ColoredOutput "🚀 Quick Test: TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI" $Cyan
    Write-Host ""
}

$testMethod = "SqlTestDataGenerator.Tests.ExecuteQueryWithTestDataAsyncDemoTests.TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI"

try {
    $startTime = Get-Date
    
    & dotnet test "SqlTestDataGenerator.Tests\SqlTestDataGenerator.Tests.csproj" `
        --configuration Debug `
        --framework net8.0 `
        --filter "FullyQualifiedName=$testMethod" `
        --logger "console;verbosity=minimal" `
        --no-build
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColoredOutput "✅ TC001 test completed successfully!" $Green
        Write-ColoredOutput "⏱️ Duration: $($duration.TotalSeconds.ToString('F2'))s" $Green
    } else {
        Write-ColoredOutput "❌ TC001 test failed (exit code: $LASTEXITCODE)" $Red
        Write-ColoredOutput "⏱️ Duration: $($duration.TotalSeconds.ToString('F2'))s" $Yellow
    }
}
catch {
    Write-ColoredOutput "💥 Test execution failed: $_" $Red
    exit 1
}

# =====================================================================
# USAGE:
# .\scripts\quick-test.ps1           # Normal output
# .\scripts\quick-test.ps1 -Quiet    # Minimal output
# ===================================================================== 