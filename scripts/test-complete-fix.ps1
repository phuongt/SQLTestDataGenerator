#!/usr/bin/env pwsh

Write-Host "Testing Complete Workflow: Generate -> Commit -> Verify..." -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

try {
    # Build the application first
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
    Write-Host "   [ ] Database dropdown shows 'MySQL' selected" -ForegroundColor Yellow
    Write-Host "   [ ] Connection string shows MySQL localhost template" -ForegroundColor Yellow
    Write-Host "   [ ] SQL editor contains complex JOIN query" -ForegroundColor Yellow
    Write-Host "   [ ] Commit button is DISABLED and gray" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 2: TEST CONNECTION" -ForegroundColor White
    Write-Host "   [ ] Click 'Test Connection' button" -ForegroundColor Yellow
    Write-Host "   [ ] Should show 'Connection successful!'" -ForegroundColor Yellow
    Write-Host "   [ ] Commit button should remain DISABLED" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 3: GENERATE TEST DATA" -ForegroundColor White
    Write-Host "   [ ] Click 'Generate Test Data' button" -ForegroundColor Yellow
    Write-Host "   [ ] Should show AI generation progress" -ForegroundColor Yellow
    Write-Host "   [ ] Should show preview results in DataGridView" -ForegroundColor Yellow
    Write-Host "   [ ] Commit button should become ENABLED and green" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 4: COMMIT DATA TO DATABASE" -ForegroundColor White
    Write-Host "   [ ] Click 'Commit' button (should be green and enabled)" -ForegroundColor Yellow
    Write-Host "   [ ] Should show success message" -ForegroundColor Yellow
    Write-Host "   [ ] Commit button should become DISABLED after success" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 5: VERIFY DATA WAS INSERTED" -ForegroundColor White
    Write-Host "   [ ] Click 'Run Query' button" -ForegroundColor Yellow
    Write-Host "   [ ] Should return ACTUAL RECORDS (not empty result)" -ForegroundColor Yellow
    Write-Host "   [ ] Records should match query criteria" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "EXPECTED RESULTS:" -ForegroundColor Yellow
    Write-Host "=================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "DEFAULT VALUES: All correct (MySQL, connection, query)" -ForegroundColor Green
    Write-Host "CONNECTION: Successful test" -ForegroundColor Green
    Write-Host "GENERATION: Creates preview data with proper file output" -ForegroundColor Green
    Write-Host "COMMIT: Saves data permanently and disables button correctly" -ForegroundColor Green
    Write-Host "VERIFICATION: Query returns actual matching records" -ForegroundColor Green
    Write-Host ""
    
    # Launch the application
    Write-Host "Launching application..." -ForegroundColor Green
    Start-Process -FilePath "SqlTestDataGenerator.UI\bin\Debug\net8.0-windows\SqlTestDataGenerator.UI.exe" -WorkingDirectory "."
    
    Write-Host ""
    Write-Host "Application launched! Follow the checklist above." -ForegroundColor Green
    Write-Host "Close the application when testing is complete." -ForegroundColor Gray
    Write-Host ""
    
} catch {
    Write-Host "Error during testing setup:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Gray
}

Write-Host "CRITICAL ITEMS TO VERIFY:" -ForegroundColor Red
Write-Host "=========================" -ForegroundColor Red
Write-Host ""
Write-Host "Commit button MUST disable after successful commit" -ForegroundColor Red
Write-Host "Query MUST return records after commit (not empty)" -ForegroundColor Red
Write-Host "Default values MUST be MySQL with proper connection" -ForegroundColor Red
Write-Host ""
Write-Host "TESTING SETUP COMPLETE!" -ForegroundColor Yellow 