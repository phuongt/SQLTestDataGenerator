# =====================================================================
# Full Workflow Integration Test Script
# =====================================================================
# PURPOSE: Test complete Generate → Export → Commit workflow
# USAGE: .\scripts\full-workflow-test.ps1
# =====================================================================

param(
    [switch]$Verbose = $false,
    [switch]$Quiet = $false
)

# Colors for output
$script:Green = [System.ConsoleColor]::Green
$script:Red = [System.ConsoleColor]::Red
$script:Yellow = [System.ConsoleColor]::Yellow
$script:Cyan = [System.ConsoleColor]::Cyan
$script:Blue = [System.ConsoleColor]::Blue

function Write-ColoredOutput {
    param(
        [string]$Message, 
        [System.ConsoleColor]$Color = [System.ConsoleColor]::White
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Section {
    param([string]$Title)
    if (-not $Quiet) {
        Write-Host ""
        $separator = "=" * 70
        Write-ColoredOutput $separator $Cyan
        Write-ColoredOutput " $Title" $Cyan
        Write-ColoredOutput $separator $Cyan
    }
}

function Test-Prerequisites {
    Write-Section "CHECKING PREREQUISITES"
    
    # Check if project can build
    Write-ColoredOutput "🔧 Building project..." $Blue
    $buildResult = & dotnet build "SqlTestDataGenerator.Tests\SqlTestDataGenerator.Tests.csproj" --configuration Debug
    
    if ($LASTEXITCODE -ne 0) {
        Write-ColoredOutput "❌ Build failed! Cannot run tests." $Red
        exit 1
    }
    
    Write-ColoredOutput "✅ Build successful" $Green
    
    # Check export directory
    $exportDir = "sql-exports"
    if (-not (Test-Path $exportDir)) {
        New-Item -ItemType Directory -Path $exportDir -Force | Out-Null
        Write-ColoredOutput "📁 Created export directory: $exportDir" $Green
    } else {
        Write-ColoredOutput "📁 Export directory exists: $exportDir" $Green
    }
}

function Run-FullWorkflowTest {
    Write-Section "RUNNING FULL WORKFLOW INTEGRATION TEST"
    
    $testMethod = "SqlTestDataGenerator.Tests.FullWorkflowIntegrationTests.TC_FULL_WORKFLOW_001_ComplexSQL_GenerateExportCommit"
    
    Write-ColoredOutput "🚀 Starting Full Workflow Test..." $Blue
    Write-ColoredOutput "   Test: TC_FULL_WORKFLOW_001" $Blue
    Write-ColoredOutput "   Workflow: Generate → Export → Commit → Verify" $Blue
    
    $startTime = Get-Date
    
    try {
        if ($Verbose) {
            $verbosity = "detailed"
        } else {
            $verbosity = "normal"
        }
        
        & dotnet test "SqlTestDataGenerator.Tests\SqlTestDataGenerator.Tests.csproj" `
            --configuration Debug `
            --framework net8.0 `
            --filter "FullyQualifiedName=$testMethod" `
            --logger "console;verbosity=$verbosity" `
            --no-build
        
        $endTime = Get-Date
        $duration = $endTime - $startTime
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColoredOutput "🎉 FULL WORKFLOW TEST PASSED!" $Green
            Write-ColoredOutput "⏱️ Duration: $($duration.TotalSeconds.ToString('F2'))s" $Green
            return $true
        } else {
            Write-ColoredOutput "❌ FULL WORKFLOW TEST FAILED!" $Red
            Write-ColoredOutput "⏱️ Duration: $($duration.TotalSeconds.ToString('F2'))s" $Yellow
            return $false
        }
    }
    catch {
        Write-ColoredOutput "💥 Test execution failed: $_" $Red
        return $false
    }
}

function Show-Results {
    param([bool]$Success)
    
    Write-Section "TEST RESULTS SUMMARY"
    
    if ($Success) {
        Write-ColoredOutput "🎯 FULL WORKFLOW INTEGRATION TEST" $Green
        Write-ColoredOutput "✅ Phase 1: Generate Complex SQL with AI" $Green
        Write-ColoredOutput "✅ Phase 2: Export SQL to File" $Green  
        Write-ColoredOutput "✅ Phase 3: Execute SQL from File" $Green
        Write-ColoredOutput "✅ Phase 4: Commit to Database" $Green
        Write-ColoredOutput "✅ Phase 5: Verify Data Persistence" $Green
        Write-ColoredOutput "✅ Phase 6: Validate Complex Query Results" $Green
        Write-Host ""
        Write-ColoredOutput "🎉 ALL PHASES COMPLETED SUCCESSFULLY!" $Green
        
        # Show generated files
        $exportDir = "sql-exports"
        if (Test-Path $exportDir) {
            $sqlFiles = Get-ChildItem $exportDir -Filter "*.sql" | Sort-Object LastWriteTime -Descending | Select-Object -First 3
            if ($sqlFiles) {
                Write-ColoredOutput "📁 Recent SQL Export Files:" $Blue
                foreach ($file in $sqlFiles) {
                    Write-ColoredOutput "   - $($file.Name) ($($file.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')))" $Blue
                }
            }
        }
    } else {
        Write-ColoredOutput "❌ FULL WORKFLOW TEST FAILED" $Red
        Write-ColoredOutput "   Check console output above for details" $Yellow
        Write-ColoredOutput "   Common issues:" $Yellow
        Write-ColoredOutput "   • Database connection problems" $Yellow
        Write-ColoredOutput "   • AI API key issues" $Yellow
        Write-ColoredOutput "   • File permission problems" $Yellow
        Write-ColoredOutput "   • SQL syntax or constraint errors" $Yellow
    }
}

# =====================================================================
# MAIN EXECUTION
# =====================================================================

if (-not $Quiet) {
    Write-ColoredOutput "🧪 FULL WORKFLOW INTEGRATION TEST RUNNER" $Cyan
    Write-ColoredOutput "Testing: Generate → Export → Commit → Verify" $Cyan
}

try {
    # Step 1: Prerequisites
    Test-Prerequisites
    
    # Step 2: Run test
    $testResult = Run-FullWorkflowTest
    
    # Ensure testResult is boolean
    $testPassed = [bool]$testResult
    
    # Step 3: Show results
    Show-Results -Success $testPassed
    
    # Exit with appropriate code
    if ($testPassed) {
        exit 0
    } else {
        exit 1
    }
}
catch {
    Write-ColoredOutput "💥 Script execution failed: $_" $Red
    exit 1
}

# =====================================================================
# USAGE EXAMPLES:
# .\scripts\full-workflow-test.ps1                 # Normal run
# .\scripts\full-workflow-test.ps1 -Verbose        # Detailed output
# .\scripts\full-workflow-test.ps1 -Quiet          # Minimal output
# ===================================================================== 