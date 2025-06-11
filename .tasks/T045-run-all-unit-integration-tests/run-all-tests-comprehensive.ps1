# PowerShell Script to run all Unit Tests and Integration Tests
# File: run-all-tests-comprehensive.ps1

param(
    [string]$OutputDir = "TestResults/ALL_$(Get-Date -Format 'yyyyMMddTHHmmss')",
    [switch]$Verbose = $false,
    [switch]$SkipDatabaseCheck = $false,
    [switch]$UnitTestsOnly = $false,
    [switch]$IntegrationTestsOnly = $false,
    [switch]$ContinueOnFailure = $false
)

Write-Host "============================================================" -ForegroundColor Blue
Write-Host "RUNNING COMPLETE TEST SUITE - SqlTestDataGenerator" -ForegroundColor Blue
Write-Host "============================================================" -ForegroundColor Blue
Write-Host "Start time: $(Get-Date)" -ForegroundColor Yellow

# Initialize results tracking
$overallResults = @{
    StartTime = Get-Date
    UnitTests = @{ Total = 0; Passed = 0; Failed = 0; Results = @() }
    IntegrationTests = @{ Total = 0; Passed = 0; Failed = 0; Results = @() }
    OverallStatus = "UNKNOWN"
}

# Create main output directory
$mainOutputPath = "SqlTestDataGenerator.Tests/$OutputDir"
if (!(Test-Path $mainOutputPath)) {
    New-Item -ItemType Directory -Path $mainOutputPath -Force | Out-Null
}

# Function to run Unit Tests
function Run-UnitTests {
    Write-Host "`n=== STARTING UNIT TESTS ===" -ForegroundColor Green
    
    $unitTestFiles = @(
        "SqlQueryParserTests.cs",
        "SqlQueryParserV2Tests.cs", 
        "SqlQueryParserV3Tests.cs",
        "SqlQueryParserDebugTests.cs",
        "ConfigurationServiceTests.cs",
        "LoggerServiceTests.cs",
        "ConstraintAwareGenerationTests.cs",
        "ComplexDataGenerationTests.cs",
        "ComplexSqlGenerationTests.cs",
        "ComprehensiveConstraintExtractorTests.cs",
        "EnhancedConstraintExtractionTests.cs",
        "ExistsConstraintExtractionTests.cs",
        "MissingSqlPatternsTests.cs",
        "RecordCountVerificationTests.cs",
        "AssertFailBehaviorDemo.cs",
        "UnitTest1.cs"
    )
    
    $overallResults.UnitTests.Total = $unitTestFiles.Count
    Write-Host "Total Unit Test files: $($unitTestFiles.Count)" -ForegroundColor Cyan
    
    foreach ($testFile in $unitTestFiles) {
        $testName = [System.IO.Path]::GetFileNameWithoutExtension($testFile)
        Write-Host "`n  Running: $testFile" -ForegroundColor Yellow
        
        try {
            Push-Location "SqlTestDataGenerator.Tests"
            
            $outputFile = "$OutputDir/UT_${testName}_Result.txt"
            $command = "dotnet test --filter `"FullyQualifiedName~$testName`" --verbosity normal --logger `"console;verbosity=detailed`""
            
            if ($Verbose) {
                Write-Host "    Command: $command" -ForegroundColor Gray
            }
            
            $testResult = Invoke-Expression "$command > `"$outputFile`" 2>&1; `$LASTEXITCODE"
            
            if ($testResult -eq 0) {
                Write-Host "    PASSED: $testFile" -ForegroundColor Green
                $overallResults.UnitTests.Passed++
                $status = "PASSED"
            } else {
                Write-Host "    FAILED: $testFile" -ForegroundColor Red
                $overallResults.UnitTests.Failed++
                $status = "FAILED"
            }
            
            $overallResults.UnitTests.Results += [PSCustomObject]@{
                TestFile = $testFile
                Status = $status
                OutputFile = $outputFile
                ExitCode = $testResult
            }
            
        } catch {
            Write-Host "    ERROR: $testFile - $($_.Exception.Message)" -ForegroundColor Red
            $overallResults.UnitTests.Failed++
            $overallResults.UnitTests.Results += [PSCustomObject]@{
                TestFile = $testFile
                Status = "ERROR"
                OutputFile = ""
                ExitCode = -1
                Error = $_.Exception.Message
            }
        } finally {
            Pop-Location
        }
        
        # Stop if failed and not continuing
        if ($testResult -ne 0 -and -not $ContinueOnFailure) {
            Write-Host "`nStopping due to Unit Test failure and ContinueOnFailure = false" -ForegroundColor Red
            return $false
        }
    }
    
    $utSuccessRate = [math]::Round(($overallResults.UnitTests.Passed / $overallResults.UnitTests.Total) * 100, 2)
    Write-Host "`nUnit Tests Summary: $($overallResults.UnitTests.Passed)/$($overallResults.UnitTests.Total) passed ($utSuccessRate%)" -ForegroundColor Cyan
    
    return $overallResults.UnitTests.Failed -eq 0
}

# Function to run Integration Tests
function Run-IntegrationTests {
    Write-Host "`n=== STARTING INTEGRATION TESTS ===" -ForegroundColor Green
    
    # Check database connection if needed
    if (-not $SkipDatabaseCheck) {
        Write-Host "`n  Checking database connection..." -ForegroundColor Cyan
        try {
            Push-Location "SqlTestDataGenerator.Tests"
            $dbTestResult = dotnet test --filter "FullyQualifiedName~DatabaseConnectionTest" --verbosity quiet 2>&1
            if ($LASTEXITCODE -ne 0) {
                Write-Host "    WARNING: Database connection test failed" -ForegroundColor Red
            } else {
                Write-Host "    Database connection OK" -ForegroundColor Green
            }
        } catch {
            Write-Host "    WARNING: Cannot check database connection" -ForegroundColor Red
        } finally {
            Pop-Location
        }
    }
    
    $integrationTestFiles = @(
        "RealMySQLIntegrationTests.cs",
        "MySQLIntegrationDuplicateKeyTests.cs", 
        "ExecuteQueryWithTestDataAsyncDemoTests.cs",
        "CreateMySQLTablesTest.cs",
        "DatabaseConnectionTest.cs",
        "RecordCountStrictVerificationTests.cs",
        "SqlDateAddUTF8Tests.cs",
        "MySQLDateFunctionConverterTests.cs",
        "DuplicateKeyBugFixTests.cs",
        "AIIntegrationBasicTest.cs"
    )
    
    $overallResults.IntegrationTests.Total = $integrationTestFiles.Count
    Write-Host "Total Integration Test files: $($integrationTestFiles.Count)" -ForegroundColor Cyan
    
    foreach ($testFile in $integrationTestFiles) {
        $testName = [System.IO.Path]::GetFileNameWithoutExtension($testFile)
        Write-Host "`n  Running: $testFile" -ForegroundColor Yellow
        
        try {
            Push-Location "SqlTestDataGenerator.Tests"
            
            $outputFile = "$OutputDir/IT_${testName}_Result.txt"
            $command = "dotnet test --filter `"FullyQualifiedName~$testName`" --verbosity normal --logger `"console;verbosity=detailed`""
            
            if ($Verbose) {
                Write-Host "    Command: $command" -ForegroundColor Gray
            }
            
            # Run with timeout
            $job = Start-Job -ScriptBlock {
                param($cmd, $output)
                Invoke-Expression "$cmd > `"$output`" 2>&1; `$LASTEXITCODE"
            } -ArgumentList $command, $outputFile
            
            $testResult = Wait-Job $job -Timeout 300
            
            if ($testResult) {
                $exitCode = Receive-Job $job
                Remove-Job $job
                
                if ($exitCode -eq 0) {
                    Write-Host "    PASSED: $testFile" -ForegroundColor Green
                    $overallResults.IntegrationTests.Passed++
                    $status = "PASSED"
                } else {
                    Write-Host "    FAILED: $testFile (Exit Code: $exitCode)" -ForegroundColor Red
                    $overallResults.IntegrationTests.Failed++
                    $status = "FAILED"
                }
            } else {
                Stop-Job $job
                Remove-Job $job
                Write-Host "    TIMEOUT: $testFile (>5 minutes)" -ForegroundColor Red
                $overallResults.IntegrationTests.Failed++
                $status = "TIMEOUT"
                $exitCode = -999
            }
            
            $overallResults.IntegrationTests.Results += [PSCustomObject]@{
                TestFile = $testFile
                Status = $status
                OutputFile = $outputFile
                ExitCode = $exitCode
            }
            
        } catch {
            Write-Host "    ERROR: $testFile - $($_.Exception.Message)" -ForegroundColor Red
            $overallResults.IntegrationTests.Failed++
            $overallResults.IntegrationTests.Results += [PSCustomObject]@{
                TestFile = $testFile
                Status = "ERROR"
                OutputFile = ""
                ExitCode = -1
                Error = $_.Exception.Message
            }
        } finally {
            Pop-Location
        }
        
        # Stop if failed and not continuing
        if ($exitCode -ne 0 -and -not $ContinueOnFailure) {
            Write-Host "`nStopping due to Integration Test failure and ContinueOnFailure = false" -ForegroundColor Red
            return $false
        }
    }
    
    $itSuccessRate = [math]::Round(($overallResults.IntegrationTests.Passed / $overallResults.IntegrationTests.Total) * 100, 2)
    Write-Host "`nIntegration Tests Summary: $($overallResults.IntegrationTests.Passed)/$($overallResults.IntegrationTests.Total) passed ($itSuccessRate%)" -ForegroundColor Cyan
    
    return $overallResults.IntegrationTests.Failed -eq 0
}

# Execute tests based on options
$unitTestSuccess = $true
$integrationTestSuccess = $true

if (-not $IntegrationTestsOnly) {
    $unitTestSuccess = Run-UnitTests
}

if (-not $UnitTestsOnly -and ($unitTestSuccess -or $ContinueOnFailure)) {
    $integrationTestSuccess = Run-IntegrationTests
}

# Calculate overall results
$overallResults.EndTime = Get-Date
$overallResults.Duration = $overallResults.EndTime - $overallResults.StartTime

$totalTests = $overallResults.UnitTests.Total + $overallResults.IntegrationTests.Total
$totalPassed = $overallResults.UnitTests.Passed + $overallResults.IntegrationTests.Passed
$totalFailed = $overallResults.UnitTests.Failed + $overallResults.IntegrationTests.Failed

if ($totalFailed -eq 0) {
    $overallResults.OverallStatus = "SUCCESS"
} else {
    $overallResults.OverallStatus = "FAILURE"
}

# Create comprehensive report
$reportFile = "$mainOutputPath/COMPREHENSIVE_Test_Report.txt"
$reportLines = @()
$reportLines += "============================================================"
$reportLines += "COMPREHENSIVE REPORT - SqlTestDataGenerator Test Suite"
$reportLines += "============================================================"
$reportLines += "Start time: $($overallResults.StartTime)"
$reportLines += "End time: $($overallResults.EndTime)"
$reportLines += "Duration: $($overallResults.Duration.ToString("hh\:mm\:ss"))"
$reportLines += ""
$reportLines += "OVERALL RESULTS"
$reportLines += "Status: $($overallResults.OverallStatus)"
$reportLines += "Total tests: $totalTests"
$reportLines += "Tests passed: $totalPassed"
$reportLines += "Tests failed: $totalFailed"
$reportLines += "Success rate: $([math]::Round(($totalPassed / $totalTests) * 100, 2))%"
$reportLines += ""
$reportLines += "UNIT TESTS"
$reportLines += "Total: $($overallResults.UnitTests.Total)"
$reportLines += "Passed: $($overallResults.UnitTests.Passed)"
$reportLines += "Failed: $($overallResults.UnitTests.Failed)"
$reportLines += "Rate: $([math]::Round(($overallResults.UnitTests.Passed / $overallResults.UnitTests.Total) * 100, 2))%"
$reportLines += ""
$reportLines += "INTEGRATION TESTS"
$reportLines += "Total: $($overallResults.IntegrationTests.Total)"
$reportLines += "Passed: $($overallResults.IntegrationTests.Passed)"
$reportLines += "Failed: $($overallResults.IntegrationTests.Failed)"
$reportLines += "Rate: $([math]::Round(($overallResults.IntegrationTests.Passed / $overallResults.IntegrationTests.Total) * 100, 2))%"
$reportLines += ""
$reportLines += "=== UNIT TEST DETAILS ==="

foreach ($result in $overallResults.UnitTests.Results) {
    $line = "$($result.Status.PadRight(8)) - $($result.TestFile)"
    if ($result.Error) {
        $line += " (Error: $($result.Error))"
    }
    $reportLines += $line
}

$reportLines += ""
$reportLines += "=== INTEGRATION TEST DETAILS ==="

foreach ($result in $overallResults.IntegrationTests.Results) {
    $line = "$($result.Status.PadRight(8)) - $($result.TestFile)"
    if ($result.Error) {
        $line += " (Error: $($result.Error))"
    }
    $reportLines += $line
}

$reportLines += ""
$reportLines += "=== NOTES ==="
$reportLines += "* Unit Tests: Logic and algorithm testing"
$reportLines += "* Integration Tests: Require valid database connection"
$reportLines += "* Detailed output for each test in folder: $OutputDir"
$reportLines += "* Use -ContinueOnFailure to continue when errors occur"
$reportLines += "* Use -Verbose to see command details"

$reportLines | Out-File -FilePath $reportFile -Encoding UTF8

# Display final results
Write-Host "`n============================================================" -ForegroundColor Blue
Write-Host "FINAL RESULTS" -ForegroundColor Blue
Write-Host "============================================================" -ForegroundColor Blue
Write-Host "Duration: $($overallResults.Duration.ToString("hh\:mm\:ss"))" -ForegroundColor Yellow
Write-Host "Summary: $totalPassed/$totalTests tests passed ($([math]::Round(($totalPassed / $totalTests) * 100, 2))%)" -ForegroundColor Cyan

if ($overallResults.UnitTests.Total -gt 0) {
    Write-Host "Unit Tests: $($overallResults.UnitTests.Passed)/$($overallResults.UnitTests.Total) passed" -ForegroundColor Green
}

if ($overallResults.IntegrationTests.Total -gt 0) {
    Write-Host "Integration Tests: $($overallResults.IntegrationTests.Passed)/$($overallResults.IntegrationTests.Total) passed" -ForegroundColor Green
}

Write-Host "Detailed results: $mainOutputPath" -ForegroundColor Yellow
Write-Host "Comprehensive report: $reportFile" -ForegroundColor Yellow

if ($overallResults.OverallStatus -eq "SUCCESS") {
    Write-Host "`nALL TESTS SUCCESSFUL!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n$totalFailed TESTS FAILED!" -ForegroundColor Red
    exit 1
} 