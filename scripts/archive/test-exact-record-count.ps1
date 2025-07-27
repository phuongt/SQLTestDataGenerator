#!/usr/bin/env pwsh

Write-Host "🔬 Testing Exact Record Count Assertions" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Change to test directory
Set-Location -Path "SqlTestDataGenerator.Tests"

Write-Host "`n📋 Running Oracle Complex Query Test..." -ForegroundColor Yellow
try {
    dotnet test --filter "TestComplexQueryPhương1989_ShouldGenerateDataAndExecute" --logger "console;verbosity=normal" --no-build
    Write-Host "✅ Oracle test completed" -ForegroundColor Green
} catch {
    Write-Host "❌ Oracle test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n📋 Running Record Count Verification Tests..." -ForegroundColor Yellow
try {
    dotnet test --filter "TestCategory=RecordCount" --logger "console;verbosity=normal" --no-build
    Write-Host "✅ Record count tests completed" -ForegroundColor Green
} catch {
    Write-Host "❌ Record count tests failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎯 Testing Summary:" -ForegroundColor Cyan
Write-Host "- Fixed assertions to use Assert.AreEqual for exact count matching" -ForegroundColor White
Write-Host "- Changed from '>=' to '=' comparison for GeneratedRecords" -ForegroundColor White
Write-Host "- Ensures data generation produces EXACTLY the requested count" -ForegroundColor White

Write-Host "`n✅ All tests should now enforce exact record count requirements!" -ForegroundColor Green

# Return to project root
Set-Location -Path ".." 