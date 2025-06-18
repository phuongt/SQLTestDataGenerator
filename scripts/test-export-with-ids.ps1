Write-Host "=================================" -ForegroundColor Cyan
Write-Host "TEST EXPORT SQL FILE WITH IDs" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Build solution
Write-Host "🔧 Building solution..." -ForegroundColor Yellow
$buildResult = dotnet build --verbosity minimal 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful!" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}

Write-Host ""
Write-Host "🧪 TESTING NEW FEATURE: SQL Export WITH ID Columns" -ForegroundColor Green
Write-Host ""

Write-Host "📋 WHAT'S NEW:" -ForegroundColor White
Write-Host "• Export SQL files now include ID columns with sequential values (1,2,3...)" -ForegroundColor Gray
Write-Host "• Auto-increment columns are explicitly included in INSERT statements" -ForegroundColor Gray
Write-Host "• Foreign key references will now work correctly in exported files" -ForegroundColor Gray
Write-Host "• SQL files include AUTO_INCREMENT reset commands for clean execution" -ForegroundColor Gray
Write-Host ""

Write-Host "🔧 IMPLEMENTATION DETAILS:" -ForegroundColor White
Write-Host "• CommonInsertBuilder.BuildInsertStatementWithIds() - new method" -ForegroundColor Gray
Write-Host "• EngineService.ExportSqlToFileWithIdsAsync() - enhanced export" -ForegroundColor Gray
Write-Host "• Parsing logic to rebuild INSERT statements with ID values" -ForegroundColor Gray
Write-Host "• Smart ID tracking per table for correct sequential assignment" -ForegroundColor Gray
Write-Host ""

Write-Host "📁 EXPECTED FILE CHANGES:" -ForegroundColor White
Write-Host "Old format:" -ForegroundColor Gray
Write-Host "  INSERT INTO \`companies\` (\`name\`, \`code\`...) VALUES (...);" -ForegroundColor DarkGray
Write-Host ""
Write-Host "New format:" -ForegroundColor Gray
Write-Host "  INSERT INTO \`companies\` (\`id\`, \`name\`, \`code\`...) VALUES (1, ...);" -ForegroundColor Green
Write-Host "  INSERT INTO \`companies\` (\`id\`, \`name\`, \`code\`...) VALUES (2, ...);" -ForegroundColor Green
Write-Host ""

Write-Host "🚀 READY TO TEST:" -ForegroundColor Yellow
Write-Host "1. Launch UI app" -ForegroundColor White
Write-Host "2. Generate some test data" -ForegroundColor White
Write-Host "3. Check exported SQL file in sql-exports/ folder" -ForegroundColor White
Write-Host "4. Verify ID columns are included with values 1,2,3..." -ForegroundColor White
Write-Host ""

Write-Host "⚡ Launching UI app for testing..." -ForegroundColor Yellow
Start-Process "SqlTestDataGenerator.UI\bin\Debug\net8.0-windows\SqlTestDataGenerator.UI.exe"

Write-Host ""
Write-Host "✅ UI app launched!" -ForegroundColor Green
Write-Host "👉 Generate data and check sql-exports/ folder for files with ID columns!" -ForegroundColor Cyan
Write-Host ""

Write-Host "📂 SQL Export files will have header comment:" -ForegroundColor White
Write-Host "   -- Generated SQL INSERT statements (WITH ID COLUMNS)" -ForegroundColor Green
Write-Host "   -- Note: ID columns included with sequential values (1,2,3...)" -ForegroundColor Green 