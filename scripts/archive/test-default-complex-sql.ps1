# Test Default Complex SQL Feature
# Kiểm tra xem khi thay đổi database type có load đúng câu SQL phức tạp tương ứng không

Write-Host "=== Testing Default Complex SQL Feature ===" -ForegroundColor Green

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build SqlTestDataGenerator.sln --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful" -ForegroundColor Green

# Run the specific test for default complex SQL
Write-Host "Running Default Complex SQL tests..." -ForegroundColor Yellow
dotnet test SqlTestDataGenerator.Tests --filter "TestClass=DefaultComplexSQLTests" --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Default Complex SQL tests failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Default Complex SQL tests passed" -ForegroundColor Green

# Run a broader test to ensure no regressions
Write-Host "Running broader test suite to check for regressions..." -ForegroundColor Yellow
dotnet test SqlTestDataGenerator.Tests --filter "TestCategory=RealMySQL|RealOracle" --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Some integration tests failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Integration tests passed" -ForegroundColor Green

Write-Host "`n=== Test Summary ===" -ForegroundColor Green
Write-Host "✅ Default Complex SQL feature implemented successfully" -ForegroundColor Green
Write-Host "✅ Database-specific syntax validation passed" -ForegroundColor Green
Write-Host "✅ No regressions detected" -ForegroundColor Green

Write-Host "`n=== Feature Details ===" -ForegroundColor Cyan
Write-Host "• MySQL: Uses DATE_ADD(NOW(), INTERVAL), LIMIT syntax" -ForegroundColor White
Write-Host "• Oracle: Uses SYSDATE, FETCH FIRST ROWS ONLY syntax" -ForegroundColor White
Write-Host "• SQL Server: Uses DATEADD, OFFSET FETCH syntax" -ForegroundColor White
Write-Host "• PostgreSQL: Uses CURRENT_DATE + INTERVAL, LIMIT syntax" -ForegroundColor White
Write-Host "• All include Vietnamese business logic comments" -ForegroundColor White

Write-Host "`n🎉 Default Complex SQL feature is ready for use!" -ForegroundColor Green 