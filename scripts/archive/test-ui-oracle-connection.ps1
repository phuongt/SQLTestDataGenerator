# Test Oracle Connection in UI
Write-Host "=== Testing Oracle Connection in UI ===" -ForegroundColor Green

# Build the project first
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build SqlTestDataGenerator.sln

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful" -ForegroundColor Green

# Instructions for manual testing
Write-Host "`n📋 Manual Testing Instructions:" -ForegroundColor Cyan
Write-Host "1. Run the UI application:" -ForegroundColor White
Write-Host "   dotnet run --project SqlTestDataGenerator.UI/SqlTestDataGenerator.UI.csproj" -ForegroundColor Gray
Write-Host "`n2. In the UI:" -ForegroundColor White
Write-Host "   - Select 'Oracle' from Database Type dropdown" -ForegroundColor Gray
Write-Host "   - Verify connection string is set to:" -ForegroundColor Gray
Write-Host "     Data Source=localhost:1521/XE;User Id=system;Password=22092012;" -ForegroundColor Gray
Write-Host "   - Click '🔌 Test Connection' button" -ForegroundColor Gray
Write-Host "   - Should show: '✅ Connection successful! Database ready for use.'" -ForegroundColor Green
Write-Host "`n3. Expected Behavior:" -ForegroundColor White
Write-Host "   - No error messages" -ForegroundColor Gray
Write-Host "   - Green status message" -ForegroundColor Gray
Write-Host "   - Connection test passes" -ForegroundColor Gray

Write-Host "`n🔧 If you encounter issues:" -ForegroundColor Yellow
Write-Host "1. Check Oracle XE is running: services.msc -> OracleServiceXE" -ForegroundColor White
Write-Host "2. Verify connection string in AppSettings.cs" -ForegroundColor White
Write-Host "3. Test with SQL*Plus: sqlplus system/22092012@localhost:1521/XE" -ForegroundColor White
Write-Host "4. Check firewall settings" -ForegroundColor White

Write-Host "`n=== Test Instructions Complete ===" -ForegroundColor Green 