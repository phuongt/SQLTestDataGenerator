# Test Daily API Limits Functionality
# This script tests the new daily API limit logic

Write-Host "🧪 Testing Daily API Limits..." -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

# Build the project first
Write-Host "📦 Building project..." -ForegroundColor Yellow
dotnet build SqlTestDataGenerator.sln

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed. Exiting." -ForegroundColor Red
    exit 1
}

# Run API usage statistics test
Write-Host "📊 Testing API usage statistics..." -ForegroundColor Yellow
dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --filter "TestMethod=TestApiUsageStatistics" --logger "console;verbosity=detailed"

# Run daily limit simulation test
Write-Host "🔄 Testing daily limit simulation..." -ForegroundColor Yellow
dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --filter "TestMethod=TestDailyLimitSimulation" --logger "console;verbosity=detailed"

# Run time availability test
Write-Host "⏰ Testing time availability checking..." -ForegroundColor Yellow
dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --filter "TestMethod=TestTimeAvailabilityChecking" --logger "console;verbosity=detailed"

Write-Host "✅ Daily API limits testing completed!" -ForegroundColor Green
Write-Host "Check the console output above for detailed results." -ForegroundColor Cyan 