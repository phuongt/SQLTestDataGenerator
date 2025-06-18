#!/usr/bin/env pwsh

Write-Host "🚀 Testing Application Startup with Default Values..." -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "🎯 TESTING OBJECTIVE:" -ForegroundColor Yellow
Write-Host "=====================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Verify that application starts with correct default values:" -ForegroundColor White
Write-Host "   📊 Database Type: MySQL (not SQL Server)" -ForegroundColor Gray
Write-Host "   🔗 Connection String: MySQL localhost template" -ForegroundColor Gray
Write-Host "   📄 SQL Query: Complex JOIN query for Phương user search" -ForegroundColor Gray
Write-Host ""

Write-Host "🔧 FIXES IMPLEMENTED:" -ForegroundColor Green
Write-Host "=====================" -ForegroundColor Green
Write-Host ""
Write-Host "✅ Database Type Default:" -ForegroundColor Green
Write-Host "   Changed: cboDbType.SelectedIndex = 0 (SQL Server)" -ForegroundColor Red
Write-Host "   To:      cboDbType.SelectedIndex = 1 (MySQL)" -ForegroundColor Green
Write-Host ""
Write-Host "✅ Connection String Force Update:" -ForegroundColor Green
Write-Host "   MySQL always gets default connection string" -ForegroundColor Gray
Write-Host "   No longer depends on existing settings" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ Commit Button Logic Fixed:" -ForegroundColor Green
Write-Host "   Disables properly after commit (file consumed)" -ForegroundColor Gray
Write-Host "   Only re-enables when new SQL file generated" -ForegroundColor Gray
Write-Host ""

Write-Host "📊 EXPECTED DEFAULT VALUES:" -ForegroundColor Yellow
Write-Host "===========================" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. 📊 Database Type Dropdown:" -ForegroundColor White
Write-Host "   Selected: MySQL" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. 🔗 Connection String Field:" -ForegroundColor White
Write-Host "   Value: Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. 📄 SQL Query Editor:" -ForegroundColor White
Write-Host "   Contains: Complex JOIN query with comment:" -ForegroundColor Cyan
Write-Host "   '-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc'" -ForegroundColor Cyan
Write-Host ""
Write-Host "4. 💾 Commit Button:" -ForegroundColor White
Write-Host "   State: DISABLED (gray background)" -ForegroundColor Cyan
Write-Host "   Text: '💾 Commit (No File)'" -ForegroundColor Cyan
Write-Host ""

Write-Host "🚀 LAUNCHING APPLICATION..." -ForegroundColor Yellow
Write-Host "===========================" -ForegroundColor Yellow
Write-Host ""

try {
    # Build first to ensure latest changes
    Write-Host "🔧 Building application..." -ForegroundColor White
    $buildResult = dotnet build SqlTestDataGenerator.UI --verbosity minimal --no-restore 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful!" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "🚀 Starting application..." -ForegroundColor White
        Write-Host "   Please verify the following default values in the UI:" -ForegroundColor Gray
        Write-Host ""
        Write-Host "   ✅ Database dropdown shows 'MySQL' selected" -ForegroundColor Green
        Write-Host "   ✅ Connection string shows MySQL localhost template" -ForegroundColor Green
        Write-Host "   ✅ SQL editor shows complex JOIN query" -ForegroundColor Green
        Write-Host "   ✅ Commit button is disabled and gray" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "🎯 TESTING INSTRUCTIONS:" -ForegroundColor Yellow
        Write-Host "========================" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "1. 👁️  Observe Default Values:" -ForegroundColor White
        Write-Host "   • Database Type dropdown selection" -ForegroundColor Gray
        Write-Host "   • Connection string content" -ForegroundColor Gray
        Write-Host "   • SQL query in editor" -ForegroundColor Gray
        Write-Host "   • Commit button state" -ForegroundColor Gray
        Write-Host ""
        Write-Host "2. 🔗 Test Connection:" -ForegroundColor White
        Write-Host "   • Click 'Test Connection' button" -ForegroundColor Gray
        Write-Host "   • Should attempt MySQL connection" -ForegroundColor Gray
        Write-Host ""
        Write-Host "3. 📊 Test Generate Data:" -ForegroundColor White
        Write-Host "   • Click 'Generate Test Data'" -ForegroundColor Gray
        Write-Host "   • Should enable Commit button after success" -ForegroundColor Gray
        Write-Host ""
        Write-Host "4. 💾 Test Commit Button:" -ForegroundColor White
        Write-Host "   • Click 'Commit' (if enabled)" -ForegroundColor Gray
        Write-Host "   • Should disable button after completion" -ForegroundColor Gray
        Write-Host ""
        
        # Launch the application
        Start-Process -FilePath "SqlTestDataGenerator.UI\bin\Debug\net8.0-windows\SqlTestDataGenerator.UI.exe" -WorkingDirectory "."
        
        Write-Host "📱 Application launched!" -ForegroundColor Green
        Write-Host "   Check the UI to verify default values are correct." -ForegroundColor White
        Write-Host ""
        Write-Host "⏳ Giving you time to test the application..." -ForegroundColor Yellow
        Write-Host "   Close the application window when done testing." -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        Write-Host "Build output:" -ForegroundColor Yellow
        Write-Host $buildResult -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Error launching application:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Gray
}

Write-Host ""
Write-Host "🎉 TESTING COMPLETE!" -ForegroundColor Yellow
Write-Host "====================" -ForegroundColor Yellow
Write-Host ""
Write-Host "✅ If you see the correct default values, the fix is successful!" -ForegroundColor Green
Write-Host "   + MySQL database type selected" -ForegroundColor White
Write-Host "   + MySQL connection string loaded" -ForegroundColor White
Write-Host "   + Complex SQL query in editor" -ForegroundColor White
Write-Host "   + Commit button properly disabled" -ForegroundColor White 