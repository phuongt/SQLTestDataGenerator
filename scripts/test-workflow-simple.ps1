#!/usr/bin/env pwsh

Write-Host "Testing Complete Workflow: Generate -> Commit -> Verify..." -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "TESTING OBJECTIVE:" -ForegroundColor Yellow
Write-Host "Verify the complete data generation and commit workflow" -ForegroundColor White
Write-Host ""

Write-Host "FIXES TO VERIFY:" -ForegroundColor Green
Write-Host "1. Commit Button Logic - proper enable/disable cycle" -ForegroundColor Gray
Write-Host "2. Default Values - MySQL database, connection, query" -ForegroundColor Gray
Write-Host "3. Query Data Matching - returns actual records after commit" -ForegroundColor Gray
Write-Host "4. Schema Flexibility - works with any database design" -ForegroundColor Gray
Write-Host ""

Write-Host "Building application..." -ForegroundColor White
$buildResult = dotnet build SqlTestDataGenerator.UI --verbosity minimal --no-restore 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host $buildResult -ForegroundColor Gray
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

Write-Host "MANUAL TESTING CHECKLIST:" -ForegroundColor Yellow
Write-Host "=========================" -ForegroundColor Yellow
Write-Host ""

Write-Host "STEP 1: VERIFY DEFAULT VALUES" -ForegroundColor White
Write-Host "- Database dropdown shows MySQL selected" -ForegroundColor Yellow
Write-Host "- Connection string shows MySQL localhost template" -ForegroundColor Yellow
Write-Host "- SQL editor contains complex JOIN query" -ForegroundColor Yellow
Write-Host "- Commit button is DISABLED and gray" -ForegroundColor Yellow
Write-Host ""

Write-Host "STEP 2: TEST CONNECTION" -ForegroundColor White
Write-Host "- Click Test Connection button" -ForegroundColor Yellow
Write-Host "- Should create sample tables if database empty" -ForegroundColor Yellow
Write-Host "- Should show Connection successful" -ForegroundColor Yellow
Write-Host "- Commit button should remain DISABLED" -ForegroundColor Yellow
Write-Host ""

Write-Host "STEP 3: GENERATE TEST DATA" -ForegroundColor White
Write-Host "- Click Generate Test Data button" -ForegroundColor Yellow
Write-Host "- Should show AI generation progress" -ForegroundColor Yellow
Write-Host "- Should show preview results in DataGridView" -ForegroundColor Yellow
Write-Host "- Commit button should become ENABLED and green" -ForegroundColor Yellow
Write-Host ""

Write-Host "STEP 4: COMMIT DATA TO DATABASE" -ForegroundColor White
Write-Host "- Click Commit button" -ForegroundColor Yellow
Write-Host "- Should show success message" -ForegroundColor Yellow
Write-Host "- After success, Commit button should become DISABLED" -ForegroundColor Yellow
Write-Host ""

Write-Host "STEP 5: VERIFY DATA WAS INSERTED" -ForegroundColor White
Write-Host "- Click Run Query button" -ForegroundColor Yellow
Write-Host "- Should return ACTUAL RECORDS not empty result" -ForegroundColor Yellow
Write-Host "- Records should match query criteria" -ForegroundColor Yellow
Write-Host ""

Write-Host "CRITICAL ITEMS TO VERIFY:" -ForegroundColor Red
Write-Host "- Commit button MUST disable after successful commit" -ForegroundColor Red
Write-Host "- Query MUST return records after commit not empty" -ForegroundColor Red
Write-Host "- Default values MUST be MySQL with proper connection" -ForegroundColor Red
Write-Host ""

Write-Host "Launching application..." -ForegroundColor White
Start-Process -FilePath "SqlTestDataGenerator.UI\bin\Debug\net8.0-windows\SqlTestDataGenerator.UI.exe" -WorkingDirectory "."

Write-Host "Application launched!" -ForegroundColor Green
Write-Host "Follow the checklist above to verify all fixes work correctly." -ForegroundColor White
Write-Host "Close the application when testing is complete." -ForegroundColor Gray 