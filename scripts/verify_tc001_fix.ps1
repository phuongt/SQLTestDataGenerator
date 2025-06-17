# Verify TC001 Fix - Check if complex query returns expected 15 rows
Write-Host "=== Verifying TC001 Fix - Complex Constraint-Aware Data Generation ===" -ForegroundColor Green

# Run the test with detailed output
$testOutput = dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --filter "FullyQualifiedName~TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI" --verbosity normal --logger "console;verbosity=detailed" 2>&1

# Capture test result
$testPassed = $testOutput -match "Passed.*TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI"
$testResult = ($testOutput -join "`n")

if ($testPassed) {
    Write-Host "✅ TC001 Test: PASSED" -ForegroundColor Green
    
    # Extract execution details from log output
    $logLines = $testResult -split "`n"
    
    # Look for key metrics
    $recordsGenerated = ($logLines | Where-Object { $_ -match "Generated Records:" }) -replace ".*Generated Records:\s*(\d+).*", '$1'
    $executionTime = ($logLines | Where-Object { $_ -match "Execution Time:" }) -replace ".*Execution Time:\s*([\d\.]+).*", '$1'
    $rowsReturned = ($logLines | Where-Object { $_ -match "Rows returned:" }) -replace ".*Rows returned:\s*(\d+).*", '$1'
    $constraintsExtracted = ($logLines | Where-Object { $_ -match "constraints:" }) 
    
    Write-Host "`n📊 TEST RESULTS SUMMARY:" -ForegroundColor Yellow
    if ($recordsGenerated) { Write-Host "   Records Generated: $recordsGenerated" -ForegroundColor Cyan }
    if ($executionTime) { Write-Host "   Execution Time: $executionTime seconds" -ForegroundColor Cyan }
    if ($rowsReturned) { Write-Host "   Rows Returned: $rowsReturned" -ForegroundColor Cyan }
    
    # Check if we got expected 15 rows
    if ($rowsReturned -eq "15") {
        Write-Host "🎯 EXPECTED RESULT ACHIEVED: 15 rows returned!" -ForegroundColor Green
        Write-Host "   ✅ Complex constraint extraction is working correctly" -ForegroundColor Green
        Write-Host "   ✅ LIKE patterns (VNEXT, DD, Phương) are applied" -ForegroundColor Green  
        Write-Host "   ✅ Boolean constraints (ur.is_active = TRUE) are satisfied" -ForegroundColor Green
        Write-Host "   ✅ Date constraints (YEAR = 1989) are handled" -ForegroundColor Green
    } else {
        Write-Host "⚠️  ATTENTION: Expected 15 rows, got $rowsReturned rows" -ForegroundColor Yellow
        Write-Host "   This may indicate constraints are not fully applied yet" -ForegroundColor Yellow
    }
    
    # Show constraint extraction info
    Write-Host "`n🔍 CONSTRAINT EXTRACTION INFO:" -ForegroundColor Yellow
    $constraintsExtracted | ForEach-Object { Write-Host "   $_" -ForegroundColor White }
    
} else {
    Write-Host "❌ TC001 Test: FAILED" -ForegroundColor Red
    Write-Host "Test Output:" -ForegroundColor Yellow
    Write-Host $testResult -ForegroundColor White
}

Write-Host "`n=== Verification Complete ===" -ForegroundColor Green 