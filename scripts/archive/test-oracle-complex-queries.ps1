# Test Oracle Complex Queries Script
Write-Host "=== Testing Oracle Complex Query Generation ===" -ForegroundColor Green

# Build the project first
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build SqlTestDataGenerator.sln

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful" -ForegroundColor Green

# Test Oracle connection first
Write-Host "`n🔍 Testing Oracle connection..." -ForegroundColor Yellow
dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --filter "TestConnection_RealOracle_ShouldConnect" --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Oracle connection failed! Please check Oracle server." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Oracle connection successful" -ForegroundColor Green

# Run Oracle complex query tests
Write-Host "`n🚀 Running Oracle complex query tests..." -ForegroundColor Yellow

# Test 1: Simple Oracle Query
Write-Host "`n📋 Test 1: Simple Oracle Query" -ForegroundColor Cyan
dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --filter "ExecuteQueryWithTestDataAsync_RealOracle_SimpleUsers_ShouldWork" --verbosity normal

# Test 2: Complex JOIN Query
Write-Host "`n📋 Test 2: Complex JOIN Query" -ForegroundColor Cyan
dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --filter "ExecuteQueryWithTestDataAsync_RealOracle_ComplexJoin_ShouldWork" --verbosity normal

# Test 3: Business Logic Query (Vietnamese)
Write-Host "`n📋 Test 3: Business Logic Query (Vietnamese)" -ForegroundColor Cyan
dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --filter "ExecuteQueryWithTestDataAsync_RealOracle_BusinessLogic_ShouldWork" --verbosity normal

# Test 4: Date Functions Query
Write-Host "`n📋 Test 4: Date Functions Query" -ForegroundColor Cyan
dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --filter "ExecuteQueryWithTestDataAsync_RealOracle_DateFunctions_ShouldWork" --verbosity normal

# Test 5: Performance Test (Large Dataset)
Write-Host "`n📋 Test 5: Performance Test (Large Dataset)" -ForegroundColor Cyan
dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --filter "ExecuteQueryWithTestDataAsync_RealOracle_Performance_ShouldWork" --verbosity normal

Write-Host "`n=== Oracle Complex Query Tests Complete ===" -ForegroundColor Green
Write-Host "📊 Check test results above for detailed information" -ForegroundColor White 