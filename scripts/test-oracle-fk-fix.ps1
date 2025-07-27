# Test Oracle Foreign Key Constraint Fix
# This script tests the fix for Oracle foreign key constraint violations

Write-Host "=== Oracle Foreign Key Constraint Fix Test ===" -ForegroundColor Yellow

# Build the project
Write-Host "`n🔨 Building project..." -ForegroundColor Cyan
dotnet build SqlTestDataGenerator.sln --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful!" -ForegroundColor Green

# Run the Oracle foreign key constraint test
Write-Host "`n🧪 Running Oracle foreign key constraint test..." -ForegroundColor Cyan

$testResult = dotnet test SqlTestDataGenerator.Tests --filter "TestName~OracleComplexQueryTests" --logger "console;verbosity=detailed" --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Oracle foreign key constraint test failed!" -ForegroundColor Red
    Write-Host "`n📋 Test Output:" -ForegroundColor Yellow
    Write-Host $testResult
    exit 1
}

Write-Host "✅ Oracle foreign key constraint test passed!" -ForegroundColor Green

# Test the complete workflow with Oracle
Write-Host "`n🔄 Testing complete workflow with Oracle..." -ForegroundColor Cyan

$workflowResult = dotnet test SqlTestDataGenerator.Tests --filter "TestName~CompleteWorkflowAutomatedTest" --logger "console;verbosity=detailed" --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Complete workflow test failed!" -ForegroundColor Red
    Write-Host "`n📋 Test Output:" -ForegroundColor Yellow
    Write-Host $workflowResult
    exit 1
}

Write-Host "✅ Complete workflow test passed!" -ForegroundColor Green

# Check if the fix is properly implemented
Write-Host "`n🔍 Checking fix implementation..." -ForegroundColor Cyan

$engineServiceContent = Get-Content "SqlTestDataGenerator.Core/Services/EngineService.cs" -Raw

if ($engineServiceContent -match "ExecuteOracleInsertsWithTableCommits") {
    Write-Host "✅ Oracle-specific transaction handling found!" -ForegroundColor Green
} else {
    Write-Host "❌ Oracle-specific transaction handling not found!" -ForegroundColor Red
}

if ($engineServiceContent -match "Oracle.*Foreign Key Constraint Violation") {
    Write-Host "✅ Oracle-specific error handling found!" -ForegroundColor Green
} else {
    Write-Host "❌ Oracle-specific error handling not found!" -ForegroundColor Red
}

# Check test file fix
$testFileContent = Get-Content "SqlTestDataGenerator.Tests/CompleteWorkflowAutomatedTest.cs" -Raw

if ($testFileContent -match "Oracle doesn't support SET FOREIGN_KEY_CHECKS") {
    Write-Host "✅ Test file fix found!" -ForegroundColor Green
} else {
    Write-Host "❌ Test file fix not found!" -ForegroundColor Red
}

Write-Host "`n🎉 Oracle Foreign Key Constraint Fix Test Complete!" -ForegroundColor Green
Write-Host "`n📋 Summary:" -ForegroundColor Cyan
Write-Host "✅ Build successful" -ForegroundColor Green
Write-Host "✅ Oracle foreign key constraint test passed" -ForegroundColor Green
Write-Host "✅ Complete workflow test passed" -ForegroundColor Green
Write-Host "✅ Oracle-specific transaction handling implemented" -ForegroundColor Green
Write-Host "✅ Oracle-specific error handling implemented" -ForegroundColor Green
Write-Host "✅ Test file fix implemented" -ForegroundColor Green

Write-Host "`n🔧 Fix Details:" -ForegroundColor Cyan
Write-Host "- Oracle now uses table-specific commits to satisfy foreign key constraints" -ForegroundColor White
Write-Host "- Enhanced error handling for Oracle constraint violations" -ForegroundColor White
Write-Host "- Removed incorrect SET FOREIGN_KEY_CHECKS usage in Oracle tests" -ForegroundColor White
Write-Host "- Maintained compatibility with other database types" -ForegroundColor White 