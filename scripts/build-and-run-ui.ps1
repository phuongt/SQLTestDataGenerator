# =====================================================================
# Build and Run Script for SqlTestDataGenerator.UI
# =====================================================================
# PURPOSE: Build và run Windows Forms UI application
# USAGE: .\scripts\build-and-run-ui.ps1
# REQUIREMENTS: .NET 6+ SDK, Visual Studio or VS Build Tools
# =====================================================================

param(
    [switch]$Clean = $false,
    [switch]$Release = $false,
    [string]$Framework = "net8.0-windows"
)

# Set error handling
$ErrorActionPreference = "Stop"

# Colors for output
$script:Green = [System.ConsoleColor]::Green
$script:Red = [System.ConsoleColor]::Red
$script:Yellow = [System.ConsoleColor]::Yellow
$script:Cyan = [System.ConsoleColor]::Cyan
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
    Write-ColoredOutput "  SqlTestDataGenerator.UI Build & Run Script" $Cyan  
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
    
    # Check project file exists
    $projectPath = "SqlTestDataGenerator.UI\SqlTestDataGenerator.UI.csproj"
    if (-not (Test-Path $projectPath)) {
        Write-ColoredOutput "❌ Project file not found: $projectPath" $Red
        exit 1
    }
    Write-ColoredOutput "✅ Project file found: $projectPath" $Green
    
    # Check appsettings.json exists
    $appSettingsPath = "SqlTestDataGenerator.UI\appsettings.json"
    if (Test-Path $appSettingsPath) {
        Write-ColoredOutput "✅ App settings found: $appSettingsPath" $Green
    } else {
        Write-ColoredOutput "⚠️ App settings not found: $appSettingsPath" $Yellow
    }
}

function Build-Project {
    param([string]$Configuration = "Debug")
    
    Write-ColoredOutput "🔨 Building SqlTestDataGenerator.UI ($Configuration)..." $Yellow
    
    $buildArgs = @(
        "build"
        "SqlTestDataGenerator.UI\SqlTestDataGenerator.UI.csproj"
        "--configuration", $Configuration
        "--framework", $Framework
        "--verbosity", "minimal"
    )
    
    if ($Clean) {
        Write-ColoredOutput "🧹 Cleaning previous build..." $Yellow
        dotnet clean "SqlTestDataGenerator.UI\SqlTestDataGenerator.UI.csproj" --configuration $Configuration
    }
    
    $buildResult = & dotnet @buildArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColoredOutput "✅ Build completed successfully!" $Green
    } else {
        Write-ColoredOutput "❌ Build failed with exit code: $LASTEXITCODE" $Red
        Write-ColoredOutput $buildResult $Red
        exit 1
    }
}

function Run-Application {
    param([string]$Configuration = "Debug")
    
    Write-ColoredOutput "🚀 Starting SqlTestDataGenerator.UI..." $Yellow
    Write-ColoredOutput "📍 Press Ctrl+C to stop the application" $Cyan
    Write-Host ""
    
    $runArgs = @(
        "run"
        "--project", "SqlTestDataGenerator.UI\SqlTestDataGenerator.UI.csproj"
        "--configuration", $Configuration
        "--framework", $Framework
    )
    
    try {
        & dotnet @runArgs
    }
    catch {
        Write-ColoredOutput "❌ Application failed to start: $_" $Red
        exit 1
    }
    finally {
        Write-ColoredOutput "🛑 Application stopped." $Yellow
    }
}

function Show-ProjectInfo {
    Write-ColoredOutput "📋 Project Information:" $Cyan
    Write-ColoredOutput "  • Project: SqlTestDataGenerator.UI" $White
    Write-ColoredOutput "  • Framework: $Framework" $White
    Write-ColoredOutput "  • Configuration: $(if ($Release) { 'Release' } else { 'Debug' })" $White
    Write-ColoredOutput "  • Clean Build: $(if ($Clean) { 'Yes' } else { 'No' })" $White
    Write-Host ""
}

# =====================================================================
# MAIN EXECUTION
# =====================================================================

try {
    Show-Header
    Show-ProjectInfo
    Check-Prerequisites
    
    $configuration = if ($Release) { "Release" } else { "Debug" }
    
    Build-Project -Configuration $configuration
    Run-Application -Configuration $configuration
    
    Write-ColoredOutput "✅ Script completed successfully!" $Green
}
catch {
    Write-ColoredOutput "💥 Script failed: $_" $Red
    Write-ColoredOutput "Stack trace: $($_.ScriptStackTrace)" $Red
    exit 1
}

# =====================================================================
# USAGE EXAMPLES:
# .\scripts\build-and-run-ui.ps1                    # Debug build & run
# .\scripts\build-and-run-ui.ps1 -Release           # Release build & run  
# .\scripts\build-and-run-ui.ps1 -Clean             # Clean build & run
# .\scripts\build-and-run-ui.ps1 -Clean -Release    # Clean release build & run
# ===================================================================== 