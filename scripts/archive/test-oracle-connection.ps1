# Test Oracle Connection Script
Write-Host "=== Testing Oracle Connection ===" -ForegroundColor Green

# Build the project first
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build SqlTestDataGenerator.sln

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful" -ForegroundColor Green

# Run a quick Oracle connection test
Write-Host "`nRunning Oracle connection test..." -ForegroundColor Yellow
dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --filter "TestConnection_RealOracle_ShouldConnect" --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Oracle connection test passed!" -ForegroundColor Green
} else {
    Write-Host "`n❌ Oracle connection test failed!" -ForegroundColor Red
    Write-Host "`nTroubleshooting tips:" -ForegroundColor Yellow
    Write-Host "1. Make sure Oracle XE is installed and running" -ForegroundColor White
    Write-Host "2. Check if Oracle service is started (services.msc)" -ForegroundColor White
    Write-Host "3. Verify connection string in AppSettings.cs" -ForegroundColor White
    Write-Host "4. Check firewall settings" -ForegroundColor White
    Write-Host "5. Try connecting with SQL*Plus or another tool" -ForegroundColor White
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Green 