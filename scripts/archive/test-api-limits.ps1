#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Comprehensive API Limit Testing Script
    
.DESCRIPTION
    Runs all API limit related tests to verify:
    - Daily API usage tracking
    - Rate limiting behavior  
    - Quota exceeded handling
    - Fallback behavior when AI limited
    - Mock service validation
    
.PARAMETER TestCategory
    Specific test category to run (optional)
    Valid values: "Daily-Limits", "Rate-Limiting", "Quota-Exceeded", "Fallback-Behavior", "Mock-API-Limits", "All"
    
.PARAMETER QuickTest
    Run only essential tests (faster execution)
    
.PARAMETER Verbose
    Show detailed test output
    
.EXAMPLE
    .\test-api-limits.ps1
    Run all API limit tests
    
.EXAMPLE
    .\test-api-limits.ps1 -TestCategory "Daily-Limits"
    Run only daily limits tests
    
.EXAMPLE
    .\test-api-limits.ps1 -QuickTest -Verbose
    Run essential tests with verbose output
#>

param(
    [ValidateSet("Daily-Limits", "Rate-Limiting", "Quota-Exceeded", "Fallback-Behavior", "Mock-API-Limits", "All")]
    [string]$TestCategory = "All",
    [switch]$QuickTest,
    [switch]$Verbose
)

# Colors for output
$ErrorColor = "Red"
$SuccessColor = "Green"
$InfoColor = "Cyan"
$WarningColor = "Yellow"

function Write-Header {
    param([string]$Message)
    Write-Host "`n" -NoNewline
    Write-Host "=" * 60 -ForegroundColor $InfoColor
    Write-Host " $Message" -ForegroundColor $InfoColor
    Write-Host "=" * 60 -ForegroundColor $InfoColor
}

function Write-TestResult {
    param([string]$TestName, [bool]$Success, [string]$Details = "")
    $status = if ($Success) { "✅ PASSED" } else { "❌ FAILED" }
    $color = if ($Success) { $SuccessColor } else { $ErrorColor }
    
    Write-Host "$status $TestName" -ForegroundColor $color
    if ($Details) {
        Write-Host "   $Details" -ForegroundColor Gray
    }
}

function Test-Prerequisites {
    Write-Header "Checking Prerequisites"
    
    # Check if dotnet is available
    try {
        $dotnetVersion = dotnet --version
        Write-Host "✅ .NET SDK: $dotnetVersion" -ForegroundColor $SuccessColor
    }
    catch {
        Write-Host "❌ .NET SDK not found" -ForegroundColor $ErrorColor
        exit 1
    }
    
    # Check if test project exists
    $testProject = "SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj"
    if (Test-Path $testProject) {
        Write-Host "✅ Test project found: $testProject" -ForegroundColor $SuccessColor
    }
    else {
        Write-Host "❌ Test project not found: $testProject" -ForegroundColor $ErrorColor
        exit 1
    }
    
    # Check API key configuration
    $apiKeyConfigured = $false
    
    # Check appsettings.json
    $appSettings = "SqlTestDataGenerator.UI/appsettings.json"
    if (Test-Path $appSettings) {
        $config = Get-Content $appSettings | ConvertFrom-Json
        if ($config.GeminiApiKey -and $config.GeminiApiKey -ne "your-gemini-api-key-here") {
            $apiKeyConfigured = $true
        }
    }
    
    # Check environment variables
    $envApiKey = [Environment]::GetEnvironmentVariable("GEMINI_API_KEY") -or 
                 [Environment]::GetEnvironmentVariable("GOOGLE_AI_API_KEY")
    if ($envApiKey) {
        $apiKeyConfigured = $true
    }
    
    if ($apiKeyConfigured) {
        Write-Host "✅ API key configured" -ForegroundColor $SuccessColor
    }
    else {
        Write-Host "⚠️ No API key found - some tests will use mock services" -ForegroundColor $WarningColor
    }
}

function Run-TestCategory {
    param([string]$Category, [string]$Description)
    
    Write-Header "Running $Category Tests - $Description"
    
    $testFilter = "TestCategory=$Category"
    $verboseFlag = if ($Verbose) { "--verbosity normal" } else { "--verbosity minimal" }
    
    try {
        $result = dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj `
            --filter $testFilter `
            --logger "console;verbosity=normal" `
            $verboseFlag.Split(' ') `
            --no-build `
            --no-restore
        
        $success = $LASTEXITCODE -eq 0
        Write-TestResult "$Category Tests" $success
        
        return $success
    }
    catch {
        Write-TestResult "$Category Tests" $false "Exception: $($_.Exception.Message)"
        return $false
    }
}

function Run-SpecificTest {
    param([string]$TestName, [string]$Description)
    
    Write-Host "`n🧪 Running: $TestName" -ForegroundColor $InfoColor
    
    try {
        $result = dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj `
            --filter "Name~$TestName" `
            --logger "console;verbosity=normal" `
            --no-build `
            --no-restore
        
        $success = $LASTEXITCODE -eq 0
        Write-TestResult $TestName $success $Description
        
        return $success
    }
    catch {
        Write-TestResult $TestName $false "Exception: $($_.Exception.Message)"
        return $false
    }
}

# Main execution
Write-Header "API Limit Testing Suite"
Write-Host "Test Category: $TestCategory" -ForegroundColor $InfoColor
Write-Host "Quick Test: $QuickTest" -ForegroundColor $InfoColor
Write-Host "Verbose: $Verbose" -ForegroundColor $InfoColor

# Check prerequisites
Test-Prerequisites

# Build project first
Write-Header "Building Project"
try {
    dotnet build SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --verbosity minimal
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful" -ForegroundColor $SuccessColor
    }
    else {
        Write-Host "❌ Build failed" -ForegroundColor $ErrorColor
        exit 1
    }
}
catch {
    Write-Host "❌ Build exception: $($_.Exception.Message)" -ForegroundColor $ErrorColor
    exit 1
}

# Test execution plan
$testResults = @()

if ($TestCategory -eq "All" -or $TestCategory -eq "Daily-Limits") {
    $result = Run-TestCategory "Daily-Limits" "API Usage Statistics & Daily Quota"
    $testResults += @{ Category = "Daily-Limits"; Success = $result }
}

if ($TestCategory -eq "All" -or $TestCategory -eq "Rate-Limiting") {
    $result = Run-TestCategory "Rate-Limiting" "Rate Limiting & Timing Controls"
    $testResults += @{ Category = "Rate-Limiting"; Success = $result }
}

if ($TestCategory -eq "All" -or $TestCategory -eq "Mock-API-Limits") {
    $result = Run-TestCategory "Mock-API-Limits" "Mock Service Validation"
    $testResults += @{ Category = "Mock-API-Limits"; Success = $result }
}

if ($TestCategory -eq "All" -or $TestCategory -eq "Fallback-Behavior") {
    $result = Run-TestCategory "API-Fallback" "Fallback to Bogus When AI Limited"
    $testResults += @{ Category = "API-Fallback"; Success = $result }
}

if ($TestCategory -eq "All" -or $TestCategory -eq "Quota-Exceeded") {
    $result = Run-TestCategory "Quota-Exceeded" "Quota Exceeded Scenarios"
    $testResults += @{ Category = "Quota-Exceeded"; Success = $result }
}

# Quick essential tests if requested
if ($QuickTest) {
    Write-Header "Quick Essential Tests"
    
    $essentialTests = @(
        @{ Name = "Test_DailyAPIUsageStatistics_ShouldReturnCurrentStatus"; Desc = "Daily usage tracking" },
        @{ Name = "Test_APIAvailabilityChecking_ShouldReflectCurrentStatus"; Desc = "Availability checking" },
        @{ Name = "Test_NoAPIKey_ShouldFallbackToBogusGeneration"; Desc = "No API key fallback" },
        @{ Name = "Test_NormalScenario_ShouldAllowAPICalls"; Desc = "Normal operation" }
    )
    
    foreach ($test in $essentialTests) {
        $result = Run-SpecificTest $test.Name $test.Desc
        $testResults += @{ Category = $test.Name; Success = $result }
    }
}

# Real API test (only if specifically requested)
if ($TestCategory -eq "Real-API-Test") {
    Write-Header "Real API Testing"
    Write-Host "⚠️ WARNING: This will consume real API quota!" -ForegroundColor $WarningColor
    
    $confirm = Read-Host "Do you want to proceed with real API testing? (y/N)"
    if ($confirm -eq "y" -or $confirm -eq "Y") {
        $result = Run-SpecificTest "Test_RealAPILimitDetection_CheckCurrentQuotaStatus" "Real API quota check"
        $testResults += @{ Category = "Real-API-Test"; Success = $result }
    }
    else {
        Write-Host "⏭️ Skipping real API tests" -ForegroundColor $InfoColor
    }
}

# Summary
Write-Header "Test Results Summary"

$totalTests = $testResults.Count
$passedTests = ($testResults | Where-Object { $_.Success }).Count
$failedTests = $totalTests - $passedTests

Write-Host "📊 OVERALL RESULTS:" -ForegroundColor $InfoColor
Write-Host "   Total Categories: $totalTests" -ForegroundColor $InfoColor
Write-Host "   Passed: $passedTests" -ForegroundColor $SuccessColor
Write-Host "   Failed: $failedTests" -ForegroundColor $(if ($failedTests -eq 0) { $SuccessColor } else { $ErrorColor })

if ($failedTests -eq 0) {
    Write-Host "`n🎉 ALL API LIMIT TESTS PASSED!" -ForegroundColor $SuccessColor
    Write-Host "   ✅ Daily API usage tracking working" -ForegroundColor $SuccessColor
    Write-Host "   ✅ Rate limiting properly enforced" -ForegroundColor $SuccessColor
    Write-Host "   ✅ Fallback behavior functioning" -ForegroundColor $SuccessColor
    Write-Host "   ✅ Mock services validated" -ForegroundColor $SuccessColor
}
else {
    Write-Host "`n⚠️ SOME TESTS FAILED" -ForegroundColor $WarningColor
    Write-Host "Check individual test results above for details" -ForegroundColor $InfoColor
}

# Detailed breakdown
Write-Host "`n📋 DETAILED BREAKDOWN:" -ForegroundColor $InfoColor
foreach ($result in $testResults) {
    $status = if ($result.Success) { "✅" } else { "❌" }
    $color = if ($result.Success) { $SuccessColor } else { $ErrorColor }
    Write-Host "   $status $($result.Category)" -ForegroundColor $color
}

# Recommendations
Write-Header "Recommendations"

if ($failedTests -eq 0) {
    Write-Host "🚀 API limit handling is working correctly!" -ForegroundColor $SuccessColor
    Write-Host "   • Monitor daily usage in production" -ForegroundColor $InfoColor
    Write-Host "   • Set up alerts for approaching limits" -ForegroundColor $InfoColor
    Write-Host "   • Verify fallback behavior under load" -ForegroundColor $InfoColor
}
else {
    Write-Host "🔧 ACTION ITEMS:" -ForegroundColor $WarningColor
    
    $failedCategories = $testResults | Where-Object { -not $_.Success } | Select-Object -ExpandProperty Category
    
    if ($failedCategories -contains "Daily-Limits") {
        Write-Host "   • Fix daily API usage tracking" -ForegroundColor $WarningColor
    }
    
    if ($failedCategories -contains "API-Fallback") {
        Write-Host "   • Fix fallback behavior when AI unavailable" -ForegroundColor $WarningColor
    }
    
    if ($failedCategories -contains "Mock-API-Limits") {
        Write-Host "   • Fix mock service implementation" -ForegroundColor $WarningColor
    }
}

# Exit with appropriate code
exit $(if ($failedTests -eq 0) { 0 } else { 1 }) 