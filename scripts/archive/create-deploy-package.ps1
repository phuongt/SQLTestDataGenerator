Write-Host "=================================" -ForegroundColor Cyan
Write-Host "CREATE LIGHTWEIGHT DEPLOY PACKAGE" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$deployFolder = "Deploy"
$version = "v1.0.$(Get-Date -Format 'yyyyMMdd')"
$packageName = "SqlTestDataGenerator_$version"
$packagePath = "$deployFolder\$packageName"

Write-Host "📦 Creating deployment package: $packageName" -ForegroundColor Green
Write-Host ""

# Clean and create deploy folder
if (Test-Path $deployFolder) {
    Write-Host "🧹 Cleaning existing deploy folder..." -ForegroundColor Yellow
    Remove-Item $deployFolder -Recurse -Force
}

Write-Host "📁 Creating deploy structure..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $packagePath -Force | Out-Null
New-Item -ItemType Directory -Path "$packagePath\sql-exports" -Force | Out-Null
New-Item -ItemType Directory -Path "$packagePath\logs" -Force | Out-Null

# Build release version
Write-Host "🔧 Building RELEASE version..." -ForegroundColor Yellow
$buildResult = dotnet build SqlTestDataGenerator.UI -c Release --verbosity minimal 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}
Write-Host "✅ Build successful!" -ForegroundColor Green

# Copy main executable and dependencies
Write-Host "📂 Copying application files..." -ForegroundColor Yellow
$sourceFolder = "SqlTestDataGenerator.UI\bin\Release\net8.0-windows"

# Core files
Copy-Item "$sourceFolder\SqlTestDataGenerator.UI.exe" -Destination $packagePath
Copy-Item "$sourceFolder\SqlTestDataGenerator.Core.dll" -Destination $packagePath
Copy-Item "$sourceFolder\appsettings.json" -Destination $packagePath

# Dependencies
Write-Host "📚 Copying dependencies..." -ForegroundColor Yellow
@(
    "Dapper.dll",
    "MySqlConnector.dll", 
    "System.Text.Json.dll",
    "Microsoft.Extensions.Configuration.dll",
    "Microsoft.Extensions.Configuration.Json.dll",
    "Microsoft.Extensions.Configuration.FileExtensions.dll",
    "Microsoft.Extensions.Configuration.Abstractions.dll",
    "Microsoft.Extensions.FileProviders.Abstractions.dll",
    "Microsoft.Extensions.FileProviders.Physical.dll",
    "Microsoft.Extensions.FileSystemGlobbing.dll",
    "Microsoft.Extensions.Primitives.dll",
    "Serilog.dll",
    "Serilog.Sinks.Console.dll",
    "Serilog.Sinks.File.dll"
) | ForEach-Object {
    if (Test-Path "$sourceFolder\$_") {
        Copy-Item "$sourceFolder\$_" -Destination $packagePath
        Write-Host "  ✓ $_" -ForegroundColor DarkGray
    }
}

# Create run script
Write-Host "🚀 Creating run script..." -ForegroundColor Yellow
$runScript = @"
@echo off
echo ========================================
echo  SQL Test Data Generator $version
echo ========================================
echo.
echo Starting application...
echo.

REM Check if .NET 8 is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET 8.0 Runtime is required!
    echo Please download from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

REM Run the application
SqlTestDataGenerator.UI.exe

echo.
echo Application closed.
pause
"@

$runScript | Out-File -FilePath "$packagePath\Run.bat" -Encoding ASCII

# Create README
Write-Host "📝 Creating README..." -ForegroundColor Yellow
$readme = @"
# SQL Test Data Generator $version

## Mô tả
Ứng dụng Windows Forms tạo dữ liệu test cho SQL database với AI integration.

## Yêu cầu hệ thống
- Windows 10/11
- .NET 8.0 Runtime
- MySQL/SQL Server database

## Cách chạy
1. Double-click vào **Run.bat**
2. Hoặc chạy trực tiếp **SqlTestDataGenerator.UI.exe**

## Tính năng chính
✅ Generate test data với AI (Google Gemini)
✅ Support MySQL và SQL Server  
✅ Smart constraint-aware data generation
✅ Export SQL files WITH ID columns (NEW!)
✅ Foreign key constraint handling
✅ Real-time query execution và preview
✅ Rollback mechanism để preview data
✅ Enhanced commit messages với table breakdown

## File cấu hình
- **appsettings.json**: Cấu hình API keys và settings
- **sql-exports/**: Thư mục chứa file SQL exported
- **logs/**: Thư mục chứa application logs

## Hỗ trợ
- Version: $version
- Build Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
- Framework: .NET 8.0 Windows Forms

## Changelog Latest Version
🆕 **NEW**: Export SQL files include ID columns với sequential values
🆕 **FIXED**: Foreign key constraint mismatch issues  
🆕 **ENHANCED**: Smart table dependency resolution
🆕 **IMPROVED**: Multi-model Gemini rotation để tránh rate limits
"@

$readme | Out-File -FilePath "$packagePath\README.md" -Encoding UTF8

# Create version info
Write-Host "ℹ️ Creating version info..." -ForegroundColor Yellow
$versionInfo = @"
Version: $version
Build Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Build Machine: $env:COMPUTERNAME
Framework: .NET 8.0
Platform: Windows x64

Features:
- AI-Enhanced Data Generation (Google Gemini)
- MySQL & SQL Server Support
- Smart Constraint Handling
- Export SQL with ID Columns (NEW!)
- Foreign Key Constraint Resolution
- Multi-Model Rotation
- Enhanced Commit Messages
- Real-time Preview & Rollback
"@

$versionInfo | Out-File -FilePath "$packagePath\VERSION.txt" -Encoding UTF8

# Create zip package
Write-Host "📦 Creating ZIP package..." -ForegroundColor Yellow
$zipPath = "$deployFolder\$packageName.zip"
if (Get-Command "Compress-Archive" -ErrorAction SilentlyContinue) {
    Compress-Archive -Path "$packagePath\*" -DestinationPath $zipPath -Force
    Write-Host "✅ ZIP package created: $zipPath" -ForegroundColor Green
} else {
    Write-Host "⚠️ ZIP creation skipped (PowerShell 5+ required)" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "🎉 DEPLOYMENT PACKAGE CREATED!" -ForegroundColor Green
Write-Host ""
Write-Host "📂 Package Location:" -ForegroundColor White
Write-Host "   $packagePath" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Package Contents:" -ForegroundColor White
Write-Host "   📁 Application executable và dependencies" -ForegroundColor Gray
Write-Host "   📁 Configuration files" -ForegroundColor Gray
Write-Host "   📁 sql-exports/ folder" -ForegroundColor Gray
Write-Host "   📁 logs/ folder" -ForegroundColor Gray
Write-Host "   🚀 Run.bat - Easy launcher" -ForegroundColor Gray
Write-Host "   📝 README.md - Documentation" -ForegroundColor Gray
Write-Host "   ℹ️ VERSION.txt - Build info" -ForegroundColor Gray
Write-Host ""

if (Test-Path $zipPath) {
    Write-Host "📦 ZIP Package:" -ForegroundColor White
    Write-Host "   $zipPath" -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "🚀 TO DEPLOY:" -ForegroundColor Yellow
Write-Host "1. Copy folder $packageName to target machine" -ForegroundColor White
Write-Host "2. Ensure .NET 8.0 Runtime is installed" -ForegroundColor White
Write-Host "3. Run 'Run.bat' or double-click SqlTestDataGenerator.UI.exe" -ForegroundColor White
Write-Host "4. Configure database connection và API keys" -ForegroundColor White
Write-Host ""

# Open deploy folder
Write-Host "📂 Opening deploy folder..." -ForegroundColor Yellow
Start-Process "explorer.exe" -ArgumentList $deployFolder 