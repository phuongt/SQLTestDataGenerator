Write-Host "=================================" -ForegroundColor Cyan
Write-Host "CREATE SINGLE-FILE DEPLOYMENT" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$deployFolder = "Deploy-SingleFile"
$version = "v1.0.$(Get-Date -Format 'yyyyMMdd')"
$packageName = "SqlTestDataGenerator_SingleFile_$version"
$packagePath = "$deployFolder\$packageName"

Write-Host "📦 Creating SINGLE-FILE deployment: $packageName" -ForegroundColor Green
Write-Host "🎯 Target: 1 EXE file + 1 CONFIG file only!" -ForegroundColor Yellow
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

# Publish as single-file self-contained
Write-Host "🔧 Publishing SINGLE-FILE SELF-CONTAINED..." -ForegroundColor Yellow
Write-Host "⚡ All dependencies will be bundled into ONE EXE file!" -ForegroundColor Cyan

$publishResult = dotnet publish SqlTestDataGenerator.UI `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -o $packagePath `
    --verbosity minimal 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Publish failed!" -ForegroundColor Red
    Write-Host $publishResult
    exit 1
}
Write-Host "✅ Single-file publish successful!" -ForegroundColor Green

# Clean up unnecessary files, keep only EXE and config
Write-Host "🧹 Cleaning up unnecessary files..." -ForegroundColor Yellow
Get-ChildItem $packagePath -File | Where-Object { 
    $_.Name -ne "SqlTestDataGenerator.UI.exe" -and 
    $_.Name -ne "appsettings.json" -and
    $_.Extension -ne ".pdb"
} | ForEach-Object {
    Remove-Item $_.FullName -Force
    Write-Host "  🗑️ Removed: $($_.Name)" -ForegroundColor DarkGray
}

# Create appsettings.json if not exists
$configPath = "$packagePath\appsettings.json"
if (-not (Test-Path $configPath)) {
    Write-Host "📝 Creating appsettings.json..." -ForegroundColor Yellow
    $config = @{
        "GeminiApiKey" = ""
        "OpenAiApiKey" = ""
        "UseAI" = $true
        "DefaultDatabase" = "MySQL"
        "DefaultConnectionString" = "Server=localhost;Port=3306;Database=test_db;Uid=root;Pwd=your_password;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;"
        "LogLevel" = "Information"
        "MaxRecordsPerGeneration" = 1000
        "EnableConstraintValidation" = $true
        "GeminiModels" = @(
            "gemini-1.5-flash",
            "gemini-1.5-flash-001", 
            "gemini-1.5-flash-002"
        )
        "DailyApiLimits" = @{
            "gemini-1.5-flash" = 1500
            "gemini-1.5-flash-001" = 1500
            "gemini-1.5-flash-002" = 1500
        }
    }
    
    $config | ConvertTo-Json -Depth 10 | Out-File -FilePath $configPath -Encoding UTF8
}

# Create simple run script
Write-Host "🚀 Creating simple run script..." -ForegroundColor Yellow
$runScript = @"
@echo off
cls
echo ========================================
echo  SQL Test Data Generator $version
echo  SINGLE-FILE DEPLOYMENT
echo ========================================
echo.
echo Starting application...
echo Note: All dependencies are bundled in the EXE
echo.

REM Run the single-file application
SqlTestDataGenerator.UI.exe

echo.
echo Application closed.
pause
"@

$runScript | Out-File -FilePath "$packagePath\Run.bat" -Encoding ASCII

# Create minimal README
Write-Host "📝 Creating minimal README..." -ForegroundColor Yellow
$readme = @"
# SQL Test Data Generator $version - SINGLE FILE EDITION

## 🚀 SIÊU ĐỢN GIẢN - CHỈ 2 FILES!

### 📁 Nội dung package:
- **SqlTestDataGenerator.UI.exe** (≈80MB) - Ứng dụng chính với TẤT CẢ dependencies
- **appsettings.json** - File cấu hình API keys và settings
- **Run.bat** - Script chạy nhanh (optional)

### ⚡ Cách chạy:
1. **Double-click vào SqlTestDataGenerator.UI.exe** 
2. Hoặc chạy **Run.bat**

### 🔧 Cấu hình:
- Mở **appsettings.json** để config API keys và database connection
- Không cần cài .NET Runtime (đã bundle sẵn!)
- Không cần file DLL nào khác!

### ✨ Tính năng đầy đủ:
✅ AI-Enhanced Data Generation (Google Gemini)
✅ MySQL & SQL Server Support  
✅ Export SQL files WITH ID columns
✅ Smart constraint handling
✅ Foreign key resolution
✅ Multi-model rotation
✅ Real-time preview & rollback

### 📊 Thông tin:
- Version: $version
- Build: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
- Size: Single EXE ≈80MB (self-contained)
- Platform: Windows x64
- Framework: .NET 8.0 (embedded)

### 🎯 Lợi ích Single-File:
- ✅ Portable - chỉ cần copy 1 file EXE
- ✅ Không cần cài .NET Runtime
- ✅ Không có dependency hell
- ✅ Chạy offline hoàn toàn
- ✅ Deploy cực kỳ đơn giản
"@

$readme | Out-File -FilePath "$packagePath\README.md" -Encoding UTF8

# Get file sizes
$exeFile = Get-Item "$packagePath\SqlTestDataGenerator.UI.exe"
$configFile = Get-Item "$packagePath\appsettings.json"

# Create ZIP package
Write-Host "📦 Creating ZIP package..." -ForegroundColor Yellow
$zipPath = "$deployFolder\$packageName.zip"
if (Get-Command "Compress-Archive" -ErrorAction SilentlyContinue) {
    Compress-Archive -Path "$packagePath\*" -DestinationPath $zipPath -Force
    $zipFile = Get-Item $zipPath
    Write-Host "✅ ZIP package created: $zipPath" -ForegroundColor Green
} else {
    Write-Host "⚠️ ZIP creation skipped (PowerShell 5+ required)" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "🎉 SINGLE-FILE DEPLOYMENT CREATED!" -ForegroundColor Green
Write-Host ""
Write-Host "📂 Package Location:" -ForegroundColor White
Write-Host "   $packagePath" -ForegroundColor Cyan
Write-Host ""
Write-Host "📁 ULTRA-LIGHTWEIGHT CONTENTS:" -ForegroundColor White
Write-Host "   🚀 SqlTestDataGenerator.UI.exe - $([math]::Round($exeFile.Length/1MB, 1)) MB (ALL-IN-ONE)" -ForegroundColor Green
Write-Host "   ⚙️ appsettings.json - $($configFile.Length) bytes (Config)" -ForegroundColor Green
Write-Host "   📝 README.md - Documentation" -ForegroundColor Gray
Write-Host "   🎬 Run.bat - Quick launcher" -ForegroundColor Gray
Write-Host "   📁 sql-exports/ - Export folder" -ForegroundColor Gray
Write-Host "   📁 logs/ - Logs folder" -ForegroundColor Gray
Write-Host ""

if (Test-Path $zipPath) {
    Write-Host "📦 ZIP Package:" -ForegroundColor White
    Write-Host "   $zipPath ($([math]::Round($zipFile.Length/1MB, 1)) MB)" -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "🎯 DEPLOYMENT INSTRUCTIONS:" -ForegroundColor Yellow
Write-Host "1. Copy SqlTestDataGenerator.UI.exe + appsettings.json to target machine" -ForegroundColor White
Write-Host "2. Edit appsettings.json with your API keys and DB connection" -ForegroundColor White  
Write-Host "3. Double-click SqlTestDataGenerator.UI.exe" -ForegroundColor White
Write-Host "4. NO .NET installation required!" -ForegroundColor Green
Write-Host ""

Write-Host "✨ BENEFITS:" -ForegroundColor Yellow
Write-Host "• 📱 PORTABLE: Just 1 EXE file (self-contained)" -ForegroundColor Green
Write-Host "• ⚡ FAST: No dependency resolution needed" -ForegroundColor Green
Write-Host "• 🛡️ RELIABLE: No DLL conflicts or missing dependencies" -ForegroundColor Green
Write-Host "• 🌐 OFFLINE: Works without internet (except AI calls)" -ForegroundColor Green
Write-Host ""

# Open deploy folder
Write-Host "📂 Opening deploy folder..." -ForegroundColor Yellow
Start-Process "explorer.exe" -ArgumentList $deployFolder 