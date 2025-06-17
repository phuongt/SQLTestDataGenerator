# =====================================================================
# Run TC001 Test Script for SqlTestDataGenerator.Tests  
# =====================================================================
# PURPOSE: Run specific test method TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI
# USAGE: .\scripts\run-tc001-test.ps1
# REQUIREMENTS: .NET 6+ SDK, MSTest, MySQL connection, Gemini API Key
# =====================================================================

param(
    [switch]$Verbose = $false,
    [switch]$Debug = $false,
    [string]$Configuration = "Debug",
    [string]$Logger = "console;verbosity=detailed",
    [string]$Framework = "net8.0"
)

# Set error handling
$ErrorActionPreference = "Stop"

# Colors for output
$script:Green = [System.ConsoleColor]::Green
$script:Red = [System.ConsoleColor]::Red
$script:Yellow = [System.ConsoleColor]::Yellow
$script:Cyan = [System.ConsoleColor]::Cyan
$script:Blue = [System.ConsoleColor]::Blue
$script:White = [System.ConsoleColor]::White

function Write-ColoredOutput {
    param(
        [string]$Message, 
        [System.ConsoleColor]$Color = [System.ConsoleColor]::White
    )
    Write-Host $Message -ForegroundColor $Color
}

function Show-Header {
    Write-Host ""
    Write-ColoredOutput "===============================================" $Cyan
    Write-ColoredOutput "  TC001 Complex Vowis SQL Test Runner" $Cyan
    Write-ColoredOutput "===============================================" $Cyan  
    Write-Host ""
}

function Check-Prerequisites {
    Write-ColoredOutput "🔍 Checking prerequisites..." $Yellow
    
    # Check .NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-ColoredOutput "✅ .NET SDK found: $dotnetVersion" $Green
    }
    catch {
        Write-ColoredOutput "❌ .NET SDK not found. Please install .NET 6+ SDK" $Red
        exit 1
    }
    
    # Check test project file exists
    $testProjectPath = "SqlTestDataGenerator.Tests\SqlTestDataGenerator.Tests.csproj"
    if (-not (Test-Path $testProjectPath)) {
        Write-ColoredOutput "❌ Test project file not found: $testProjectPath" $Red
        exit 1
    }
    Write-ColoredOutput "✅ Test project found: $testProjectPath" $Green
    
    # Check test class file exists
    $testClassPath = "SqlTestDataGenerator.Tests\ExecuteQueryWithTestDataAsyncDemoTests.cs"
    if (-not (Test-Path $testClassPath)) {
        Write-ColoredOutput "❌ Test class file not found: $testClassPath" $Red
        exit 1
    }
    Write-ColoredOutput "✅ Test class found: $testClassPath" $Green
    
    # Check app.config for API key
    $appConfigPath = "SqlTestDataGenerator.Tests\app.config"
    if (Test-Path $appConfigPath) {
        Write-ColoredOutput "✅ App config found: $appConfigPath" $Green
        
        # Check for Gemini API key in config
        $configContent = Get-Content $appConfigPath -Raw
        if ($configContent -match 'GeminiApiKey') {
            Write-ColoredOutput "✅ Gemini API Key configuration detected" $Green
        } else {
            Write-ColoredOutput "⚠️ Gemini API Key not found in app.config" $Yellow
            Write-ColoredOutput "   Test will fallback to constraint-based generation" $Yellow
        }
    } else {
        Write-ColoredOutput "⚠️ App config not found: $appConfigPath" $Yellow
    }
}

function Build-TestProject {
    Write-ColoredOutput "🔨 Building test project..." $Yellow
    
    $buildArgs = @(
        "build"
        "SqlTestDataGenerator.Tests\SqlTestDataGenerator.Tests.csproj"
        "--configuration", $Configuration
        "--framework", $Framework
        "--verbosity", "minimal"
    )
    
    $buildResult = & dotnet @buildArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColoredOutput "✅ Test project build completed successfully!" $Green
    } else {
        Write-ColoredOutput "❌ Test project build failed with exit code: $LASTEXITCODE" $Red
        Write-ColoredOutput $buildResult $Red
        exit 1
    }
}

function Run-TC001Test {
    Write-ColoredOutput "🧪 Running TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI..." $Yellow
    Write-Host ""
    
    # Test method full name
    $testMethod = "SqlTestDataGenerator.Tests.ExecuteQueryWithTestDataAsyncDemoTests.TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI"
    
    $testArgs = @(
        "test"
        "SqlTestDataGenerator.Tests\SqlTestDataGenerator.Tests.csproj"
        "--configuration", $Configuration
        "--framework", $Framework
        "--filter", "FullyQualifiedName=$testMethod"
        "--logger", $Logger
        "--no-build"
    )
    
    if ($Verbose) {
        $testArgs += "--verbosity", "diagnostic"
    }
    
    Write-ColoredOutput "🎯 Target Test Method: $testMethod" $Blue
    Write-ColoredOutput "📋 Test Categories: AI-MySQL-Real, Integration" $Blue
    Write-ColoredOutput "🔧 Configuration: $Configuration" $Blue
    Write-Host ""
    
    Write-ColoredOutput "📝 Expected Test Behavior:" $Cyan
    Write-ColoredOutput "  • WITH Gemini API + MySQL: Generate 15 meaningful records" $White
    Write-ColoredOutput "  • WITHOUT API key: Fallback to constraint-based generation" $White  
    Write-ColoredOutput "  • WITHOUT MySQL: Fail with connection error" $White
    Write-ColoredOutput "  • Validates: AI-enhanced data generation for complex SQL" $White
    Write-Host ""
    
    try {
        $testStartTime = Get-Date
        
        & dotnet @testArgs
        
        $testEndTime = Get-Date
        $testDuration = $testEndTime - $testStartTime
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-ColoredOutput "✅ TC001 test completed successfully!" $Green
            Write-ColoredOutput "⏱️ Test Duration: $($testDuration.TotalSeconds.ToString('F2')) seconds" $Green
        } else {
            Write-Host ""
            Write-ColoredOutput "❌ TC001 test failed with exit code: $LASTEXITCODE" $Red
            Write-ColoredOutput "⏱️ Test Duration: $($testDuration.TotalSeconds.ToString('F2')) seconds" $Yellow
            
            # Provide guidance on common failures
            Write-Host ""
            Write-ColoredOutput "🔍 Common Failure Reasons:" $Yellow
            Write-ColoredOutput "  • MySQL connection not available (expected)" $White
            Write-ColoredOutput "  • Gemini API quota exceeded (engine still works)" $White
            Write-ColoredOutput "  • Missing API key (fallback to constraint generation)" $White
            Write-ColoredOutput "  • Build issues (check dependencies)" $White
        }
    }
    catch {
        Write-ColoredOutput "💥 Test execution failed: $_" $Red
        exit 1
    }
}

function Show-TestInfo {
    Write-ColoredOutput "📋 Test Information:" $Cyan
    Write-ColoredOutput "  • Test Class: ExecuteQueryWithTestDataAsyncDemoTests" $White
    Write-ColoredOutput "  • Test Method: TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI" $White
    Write-ColoredOutput "  • Categories: AI-MySQL-Real, Integration" $White
    Write-ColoredOutput "  • Framework: $Framework" $White
    Write-ColoredOutput "  • Configuration: $Configuration" $White
    Write-ColoredOutput "  • Logger: $Logger" $White
    Write-ColoredOutput "  • Debug Mode: $(if ($Debug) { 'Yes' } else { 'No' })" $White
    Write-Host ""
}

function Show-LogLocations {
    Write-ColoredOutput "📁 Log Locations:" $Blue
    Write-ColoredOutput "  • Test Results: SqlTestDataGenerator.Tests\TestResults\" $White
    Write-ColoredOutput "  • Application Logs: logs\" $White
    Write-ColoredOutput "  • Test Logs: SqlTestDataGenerator.Tests\logs\" $White
    Write-Host ""
}

# =====================================================================
# MAIN EXECUTION  
# =====================================================================

try {
    Show-Header
    Show-TestInfo
    Check-Prerequisites
    Show-LogLocations
    
    Build-TestProject
    Run-TC001Test
    
    Write-ColoredOutput "✅ Script completed!" $Green
    Write-ColoredOutput "📊 Check TestResults folder for detailed test output" $Cyan
}
catch {
    Write-ColoredOutput "💥 Script failed: $_" $Red
    Write-ColoredOutput "Stack trace: $($_.ScriptStackTrace)" $Red
    exit 1
}

# =====================================================================
# USAGE EXAMPLES:
# .\scripts\run-tc001-test.ps1                     # Run with default settings
# .\scripts\run-tc001-test.ps1 -Verbose            # Run with verbose output
# .\scripts\run-tc001-test.ps1 -Debug              # Run in debug mode
# .\scripts\run-tc001-test.ps1 -Configuration Release  # Run release build
# ===================================================================== 