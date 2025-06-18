#!/usr/bin/env pwsh

Write-Host "Testing Enhanced Commit Message with Table Breakdown & Verification..." -ForegroundColor Cyan
Write-Host "====================================================================" -ForegroundColor Cyan
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
    
    Write-Host "ENHANCED COMMIT FEATURES TO TEST:" -ForegroundColor Yellow
    Write-Host "==================================" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "1. DETAILED TABLE BREAKDOWN" -ForegroundColor White
    Write-Host "   [ ] Commit message shows INSERT count per table:" -ForegroundColor Yellow
    Write-Host "       companies: X record(s)" -ForegroundColor Gray
    Write-Host "       roles: X record(s)" -ForegroundColor Gray
    Write-Host "       users: X record(s)" -ForegroundColor Gray
    Write-Host "       user_roles: X record(s)" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "2. AUTOMATIC VERIFICATION" -ForegroundColor White
    Write-Host "   [ ] After commit, app shows 'Verifying data...' status" -ForegroundColor Yellow
    Write-Host "   [ ] App automatically runs original query" -ForegroundColor Yellow
    Write-Host "   [ ] DataGridView shows verification results" -ForegroundColor Yellow
    Write-Host "   [ ] Status shows 'Verification: X matching records found'" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "3. ENHANCED SUCCESS MESSAGE" -ForegroundColor White
    Write-Host "   [ ] Message has 'COMMIT DETAILS' section" -ForegroundColor Yellow
    Write-Host "   [ ] Message has 'VERIFICATION RESULTS' section" -ForegroundColor Yellow
    Write-Host "   [ ] Shows table breakdown with record counts" -ForegroundColor Yellow
    Write-Host "   [ ] Shows verification query results" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "TESTING WORKFLOW:" -ForegroundColor Yellow
    Write-Host "=================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "STEP 1: Generate test data" -ForegroundColor White
    Write-Host "STEP 2: Click Commit button" -ForegroundColor White
    Write-Host "STEP 3: Observe detailed commit message" -ForegroundColor White
    Write-Host "STEP 4: Verify table breakdown accuracy" -ForegroundColor White
    Write-Host "STEP 5: Verify automatic query verification" -ForegroundColor White
    Write-Host ""
    
    Write-Host "EXPECTED COMMIT MESSAGE FORMAT:" -ForegroundColor Yellow
    Write-Host "===============================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Execute SQL File thanh cong!" -ForegroundColor Green
    Write-Host ""
    Write-Host "COMMIT DETAILS:" -ForegroundColor Green
    Write-Host "• Da thuc thi X cau lenh SQL tu file" -ForegroundColor Green
    Write-Host "• File: generated_inserts_MySQL_timestamp.sql" -ForegroundColor Green
    Write-Host ""
    Write-Host "Chi tiet INSERT theo table:" -ForegroundColor Green
    Write-Host "   • companies: 5 record(s)" -ForegroundColor Green
    Write-Host "   • roles: 3 record(s)" -ForegroundColor Green
    Write-Host "   • users: 10 record(s)" -ForegroundColor Green
    Write-Host "   • user_roles: 8 record(s)" -ForegroundColor Green
    Write-Host ""
    Write-Host "VERIFICATION RESULTS:" -ForegroundColor Green
    Write-Host "• Chay lai cau query goc: X records found" -ForegroundColor Green
    Write-Host "• Trang thai: Du lieu da duoc LAN VAO DATABASE" -ForegroundColor Green
    Write-Host ""
    Write-Host "Query verification cho thay X matching records!" -ForegroundColor Green
    Write-Host ""
    
    # Launch the application
    Write-Host "Launching application for enhanced commit testing..." -ForegroundColor Green
    Start-Process -FilePath "SqlTestDataGenerator.UI\bin\Debug\net8.0-windows\SqlTestDataGenerator.UI.exe" -WorkingDirectory "."
    
    Write-Host ""
    Write-Host "Application launched!" -ForegroundColor Green
    Write-Host ""
    Write-Host "TESTING INSTRUCTIONS:" -ForegroundColor Yellow
    Write-Host "====================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Follow the standard workflow:" -ForegroundColor White
    Write-Host "   - Test Connection" -ForegroundColor Gray
    Write-Host "   - Generate Test Data" -ForegroundColor Gray
    Write-Host "   - Click Commit (watch for enhanced message)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Pay attention to commit message details:" -ForegroundColor White
    Write-Host "   - Table breakdown section" -ForegroundColor Gray
    Write-Host "   - Verification results section" -ForegroundColor Gray
    Write-Host "   - Record counts accuracy" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Verify automatic query execution:" -ForegroundColor White
    Write-Host "   - Status shows verification progress" -ForegroundColor Gray
    Write-Host "   - DataGridView updates with results" -ForegroundColor Gray
    Write-Host "   - Verification count matches expected" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "CRITICAL SUCCESS CRITERIA:" -ForegroundColor Red
    Write-Host "=========================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Commit message MUST show detailed table breakdown" -ForegroundColor Red
    Write-Host "App MUST automatically verify data after commit" -ForegroundColor Red
    Write-Host "Verification MUST show actual matching records" -ForegroundColor Red
    Write-Host "Table counts MUST be accurate and useful" -ForegroundColor Red
    Write-Host ""
    
} catch {
    Write-Host "Error during testing setup:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Gray
}

Write-Host "ENHANCED COMMIT MESSAGE TESTING READY!" -ForegroundColor Yellow
Write-Host "======================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Close the application when testing is complete." -ForegroundColor Gray 