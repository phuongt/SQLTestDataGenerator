#!/usr/bin/env pwsh

Write-Host "Testing Record Count Assertion: Desired -> Generated -> Committed -> Verified..." -ForegroundColor Cyan
Write-Host "=============================================================================" -ForegroundColor Cyan
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
    
    Write-Host "RECORD COUNT INTEGRITY TESTING OBJECTIVES:" -ForegroundColor Yellow
    Write-Host "===========================================" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "1. DESIRED vs GENERATED ASSERTION" -ForegroundColor White
    Write-Host "   [ ] Set Desired records: 15" -ForegroundColor Yellow
    Write-Host "   [ ] Generate data should create 15 total records" -ForegroundColor Yellow
    Write-Host "   [ ] Preview should show 15 records in DataGridView" -ForegroundColor Yellow
    Write-Host "   [ ] Status should report 'Generated 15 records'" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "2. GENERATED vs COMMITTED ASSERTION" -ForegroundColor White
    Write-Host "   [ ] SQL file should contain 15 INSERT statements" -ForegroundColor Yellow
    Write-Host "   [ ] Commit should execute exactly 15 statements" -ForegroundColor Yellow
    Write-Host "   [ ] Table breakdown should sum to 15 total" -ForegroundColor Yellow
    Write-Host "   [ ] Commit message should show 15 executed statements" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "3. COMMITTED vs VERIFICATION ASSERTION" -ForegroundColor White
    Write-Host "   [ ] Verification query should find matching records" -ForegroundColor Yellow
    Write-Host "   [ ] DataGridView should show results after commit" -ForegroundColor Yellow
    Write-Host "   [ ] Verification count should be > 0 (depending on query)" -ForegroundColor Yellow
    Write-Host "   [ ] Status should show 'Verification: X matching records'" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "4. COMPREHENSIVE COUNT VALIDATION" -ForegroundColor White
    Write-Host "   [ ] Companies table: Expected vs Actual count" -ForegroundColor Yellow
    Write-Host "   [ ] Roles table: Expected vs Actual count" -ForegroundColor Yellow
    Write-Host "   [ ] Users table: Expected vs Actual count" -ForegroundColor Yellow
    Write-Host "   [ ] User_roles table: Expected vs Actual count" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "TESTING WORKFLOW WITH ASSERTIONS:" -ForegroundColor Yellow
    Write-Host "=================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "STEP 1: Set Record Count to 15" -ForegroundColor White
    Write-Host "   - Change NumericUpDown to 15" -ForegroundColor Gray
    Write-Host "   - This becomes our DESIRED count baseline" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "STEP 2: Generate and Count Preview" -ForegroundColor White
    Write-Host "   - Click Generate Test Data" -ForegroundColor Gray
    Write-Host "   - Count rows in DataGridView (should be 15)" -ForegroundColor Gray
    Write-Host "   - Check status message for generation count" -ForegroundColor Gray
    Write-Host "   - Note: This is PREVIEW data (not yet committed)" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "STEP 3: Commit and Verify Execution Count" -ForegroundColor White
    Write-Host "   - Click Commit button" -ForegroundColor Gray
    Write-Host "   - Watch for 'Executing SQL file...' progress" -ForegroundColor Gray
    Write-Host "   - Check commit message for executed statement count" -ForegroundColor Gray
    Write-Host "   - Verify table breakdown sums to expected total" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "STEP 4: Verify Query Results" -ForegroundColor White
    Write-Host "   - App auto-runs verification query after commit" -ForegroundColor Gray
    Write-Host "   - Check DataGridView for matching records" -ForegroundColor Gray
    Write-Host "   - Verify status shows 'Verification: X records found'" -ForegroundColor Gray
    Write-Host "   - Note: Query results depend on filtering criteria" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "EXPECTED ASSERTIONS TO VALIDATE:" -ForegroundColor Yellow
    Write-Host "================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Generation Phase:" -ForegroundColor Green
    Write-Host "   DESIRED (15) == GENERATED (15) == PREVIEW (15)" -ForegroundColor Green
    Write-Host ""
    Write-Host "Commit Phase:" -ForegroundColor Green
    Write-Host "   GENERATED (15) == EXECUTED (15) == TABLE_SUM (15)" -ForegroundColor Green
    Write-Host ""
    Write-Host "Verification Phase:" -ForegroundColor Green
    Write-Host "   COMMITTED (15) -> QUERY_FILTER -> RESULTS (varies)" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "EXAMPLE EXPECTED RESULTS:" -ForegroundColor Yellow
    Write-Host "=========================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Desired Records: 15" -ForegroundColor Green
    Write-Host "Generation Status: 'Generated 15 records (PREVIEW ONLY)'" -ForegroundColor Green
    Write-Host "Preview DataGridView: 15 rows" -ForegroundColor Green
    Write-Host ""
    Write-Host "Commit Details:" -ForegroundColor Green
    Write-Host "   • Da thuc thi 15 cau lenh SQL tu file" -ForegroundColor Green
    Write-Host "   • companies: 3 record(s)" -ForegroundColor Green
    Write-Host "   • roles: 2 record(s)" -ForegroundColor Green
    Write-Host "   • users: 7 record(s)" -ForegroundColor Green
    Write-Host "   • user_roles: 3 record(s)" -ForegroundColor Green
    Write-Host "   Total: 3+2+7+3 = 15 ✓" -ForegroundColor Green
    Write-Host ""
    Write-Host "Verification Results:" -ForegroundColor Green
    Write-Host "   • Chay lai cau query goc: 2 records found" -ForegroundColor Green
    Write-Host "   (Note: Only 2 match complex query criteria out of 15 total)" -ForegroundColor Green
    Write-Host ""
    
    # Launch the application
    Write-Host "Launching application for record count testing..." -ForegroundColor Green
    Start-Process -FilePath "SqlTestDataGenerator.UI\bin\Debug\net8.0-windows\SqlTestDataGenerator.UI.exe" -WorkingDirectory "."
    
    Write-Host ""
    Write-Host "Application launched!" -ForegroundColor Green
    Write-Host ""
    Write-Host "TESTING INSTRUCTIONS:" -ForegroundColor Yellow
    Write-Host "====================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Set Record Count to 15 (clear baseline)" -ForegroundColor White
    Write-Host "2. Test Connection first" -ForegroundColor White
    Write-Host "3. Generate Data and count preview rows" -ForegroundColor White
    Write-Host "4. Commit and verify execution counts" -ForegroundColor White
    Write-Host "5. Check verification query results" -ForegroundColor White
    Write-Host "6. Validate all count assertions" -ForegroundColor White
    Write-Host ""
    
    Write-Host "CRITICAL VALIDATIONS:" -ForegroundColor Red
    Write-Host "====================" -ForegroundColor Red
    Write-Host ""
    Write-Host "MUST validate: Desired == Generated == Preview" -ForegroundColor Red
    Write-Host "MUST validate: Generated == Committed == Table Sum" -ForegroundColor Red
    Write-Host "MUST validate: Verification shows actual matching data" -ForegroundColor Red
    Write-Host "MUST validate: No data loss or duplication in flow" -ForegroundColor Red
    Write-Host ""
    
    Write-Host "POTENTIAL ISSUES TO WATCH FOR:" -ForegroundColor Red
    Write-Host "==============================" -ForegroundColor Red
    Write-Host ""
    Write-Host "❌ Generation creates more/less than desired" -ForegroundColor Red
    Write-Host "❌ Preview shows different count than generated" -ForegroundColor Red
    Write-Host "❌ Commit executes different count than generated" -ForegroundColor Red
    Write-Host "❌ Table breakdown doesn't sum to total" -ForegroundColor Red
    Write-Host "❌ Verification fails or shows 0 results unexpectedly" -ForegroundColor Red
    Write-Host "❌ Data duplicated or lost during commit process" -ForegroundColor Red
    Write-Host ""
    
} catch {
    Write-Host "Error during testing setup:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Gray
}

Write-Host "RECORD COUNT ASSERTION TESTING READY!" -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Test thoroughly and report any count mismatches!" -ForegroundColor Gray
Write-Host "Close the application when testing is complete." -ForegroundColor Gray 