#!/usr/bin/env pwsh

Write-Host "🔄 Testing Complete Workflow: Generate → Commit → Verify..." -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "🎯 TESTING OBJECTIVE:" -ForegroundColor Yellow
Write-Host "=====================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Verify the complete data generation and commit workflow:" -ForegroundColor White
Write-Host "   1. 🚀 Generate test data (should work with any SQL)" -ForegroundColor Gray
Write-Host "   2. 💾 Commit data to database (button should disable properly)" -ForegroundColor Gray
Write-Host "   3. 🔍 Run query to verify data was inserted correctly" -ForegroundColor Gray
Write-Host "   4. 📊 Check that results match expected records" -ForegroundColor Gray
Write-Host ""

Write-Host "🔧 FIXES TO VERIFY:" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green
Write-Host ""
Write-Host "✅ 1. Commit Button Logic:" -ForegroundColor Green
Write-Host "   • Disabled at startup (no file)" -ForegroundColor Gray
Write-Host "   • Enabled after successful generation (file available)" -ForegroundColor Gray
Write-Host "   • Disabled during execution (prevent double-click)" -ForegroundColor Gray
Write-Host "   • Disabled after completion (file consumed)" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ 2. Default Values:" -ForegroundColor Green
Write-Host "   • MySQL database type selected" -ForegroundColor Gray
Write-Host "   • MySQL connection string populated" -ForegroundColor Gray
Write-Host "   • Complex SQL query loaded" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ 3. Query Data Matching:" -ForegroundColor Green
Write-Host "   • Sample data includes companies with 'HOME'" -ForegroundColor Gray
Write-Host "   • Sample data includes roles with 'member'" -ForegroundColor Gray
Write-Host "   • Sample data includes users named 'Phương' born 1989" -ForegroundColor Gray
Write-Host "   • Query returns actual records after commit" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ 4. Schema Flexibility:" -ForegroundColor Green
Write-Host "   • Only creates sample tables if database is empty" -ForegroundColor Gray
Write-Host "   • Respects existing database schemas" -ForegroundColor Gray
Write-Host "   • Works with any SQL query structure" -ForegroundColor Gray
Write-Host ""

Write-Host "🚀 STARTING COMPREHENSIVE TEST..." -ForegroundColor Yellow
Write-Host "==================================" -ForegroundColor Yellow
Write-Host ""

try {
    # Build the application first
    Write-Host "🔧 Building application..." -ForegroundColor White
    $buildResult = dotnet build SqlTestDataGenerator.UI --verbosity minimal --no-restore 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        Write-Host $buildResult -ForegroundColor Gray
        exit 1
    }
    
    Write-Host "✅ Build successful!" -ForegroundColor Green
    Write-Host ""
    
    # Launch the application
    Write-Host "🚀 Launching application for manual testing..." -ForegroundColor White
    Write-Host ""
    
    Write-Host "📋 MANUAL TESTING CHECKLIST:" -ForegroundColor Yellow
    Write-Host "============================" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 1: 👁️  VERIFY DEFAULT VALUES" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "   ☐ Database dropdown shows 'MySQL' selected" -ForegroundColor Yellow
    Write-Host "   ☐ Connection string shows MySQL localhost template" -ForegroundColor Yellow
    Write-Host "   ☐ SQL editor contains complex JOIN query" -ForegroundColor Yellow
    Write-Host "   ☐ Commit button is DISABLED and gray" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 2: 🔗 TEST CONNECTION" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "   ☐ Click 'Test Connection' button" -ForegroundColor Yellow
    Write-Host "   ☐ Should show 'Testing MySQL connection...'" -ForegroundColor Yellow
    Write-Host "   ☐ Should create sample tables (if database empty)" -ForegroundColor Yellow
    Write-Host "   ☐ Should show 'Connection successful!'" -ForegroundColor Yellow
    Write-Host "   ☐ Commit button should remain DISABLED" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 3: 🚀 GENERATE TEST DATA" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "   ☐ Click 'Generate Test Data' button" -ForegroundColor Yellow
    Write-Host "   ☐ Should show AI generation progress" -ForegroundColor Yellow
    Write-Host "   ☐ Should show 'Generated X records (PREVIEW ONLY - ROLLBACK)'" -ForegroundColor Yellow
    Write-Host "   ☐ DataGridView should show preview results" -ForegroundColor Yellow
    Write-Host "   ☐ Commit button should become ENABLED and green" -ForegroundColor Yellow
    Write-Host "   ☐ Button text should show 'Commit: filename.sql'" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 4: 💾 COMMIT DATA TO DATABASE" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "   ☐ Click 'Commit' button (should be green and enabled)" -ForegroundColor Yellow
    Write-Host "   ☐ Should show 'Executing SQL file...' during process" -ForegroundColor Yellow
    Write-Host "   ☐ Should show success message: 'Execute SQL File thành công!'" -ForegroundColor Yellow
    Write-Host "   ☐ Message should mention button will be disabled" -ForegroundColor Yellow
    Write-Host "   ☐ After success, Commit button should become DISABLED and gray" -ForegroundColor Yellow
    Write-Host "   ☐ Button text should show 'Commit (No File)'" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 5: 🔍 VERIFY DATA WAS INSERTED" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "   ☐ Click 'Run Query' button" -ForegroundColor Yellow
    Write-Host "   ☐ Should show 'Running query...' during execution" -ForegroundColor Yellow
    Write-Host "   ☐ Should return ACTUAL RECORDS (not empty result)" -ForegroundColor Yellow
    Write-Host "   ☐ Records should match query criteria:" -ForegroundColor Yellow
    Write-Host "       • Users with names containing 'Phương'" -ForegroundColor Gray
    Write-Host "       • Born in 1989" -ForegroundColor Gray
    Write-Host "       • Companies containing 'HOME'" -ForegroundColor Gray
    Write-Host "       • Roles containing 'member'" -ForegroundColor Gray
    Write-Host "   ☐ Status should show 'Query executed successfully'" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 6: 🔄 TEST BUTTON STATE CYCLE" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "   ☐ Commit button should still be DISABLED after query" -ForegroundColor Yellow
    Write-Host "   ☐ Generate new data to re-enable Commit button" -ForegroundColor Yellow
    Write-Host "   ☐ Verify button enables/disables correctly in cycle" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "🎯 EXPECTED RESULTS:" -ForegroundColor Yellow
    Write-Host "====================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "✅ DEFAULT VALUES: All correct (MySQL, connection, query)" -ForegroundColor Green
    Write-Host "✅ CONNECTION: Successful with appropriate table creation" -ForegroundColor Green
    Write-Host "✅ GENERATION: Creates preview data with proper file output" -ForegroundColor Green
    Write-Host "✅ COMMIT: Saves data permanently and disables button correctly" -ForegroundColor Green
    Write-Host "✅ VERIFICATION: Query returns actual matching records" -ForegroundColor Green
    Write-Host "✅ BUTTON LOGIC: Proper enable/disable cycle throughout" -ForegroundColor Green
    Write-Host ""
    
    # Launch the application
    Start-Process -FilePath "SqlTestDataGenerator.UI\bin\Debug\net8.0-windows\SqlTestDataGenerator.UI.exe" -WorkingDirectory "."
    
    Write-Host "📱 Application launched!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🔍 TESTING INSTRUCTIONS:" -ForegroundColor Yellow
    Write-Host "========================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Follow the checklist above step by step" -ForegroundColor White
    Write-Host "2. Check each ☐ box as you complete the verification" -ForegroundColor White
    Write-Host "3. Note any issues or unexpected behavior" -ForegroundColor White
    Write-Host "4. Pay special attention to button state changes" -ForegroundColor White
    Write-Host "5. Verify that query returns actual records (not empty)" -ForegroundColor White
    Write-Host ""
    Write-Host "⚠️  CRITICAL ITEMS TO VERIFY:" -ForegroundColor Red
    Write-Host "=============================" -ForegroundColor Red
    Write-Host ""
    Write-Host "🔴 Commit button MUST disable after successful commit" -ForegroundColor Red
    Write-Host "🔴 Query MUST return records after commit (not empty)" -ForegroundColor Red
    Write-Host "🔴 Default values MUST be MySQL with proper connection" -ForegroundColor Red
    Write-Host "🔴 Tool MUST work with both empty and existing databases" -ForegroundColor Red
    Write-Host ""
    
    Write-Host "⏳ Take your time to test thoroughly..." -ForegroundColor Yellow
    Write-Host "   Close the application when testing is complete." -ForegroundColor Gray
    Write-Host ""
    
} catch {
    Write-Host "❌ Error during testing setup:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Gray
}

Write-Host ""
Write-Host "📊 TESTING SCENARIOS COVERED:" -ForegroundColor Yellow
Write-Host "==============================" -ForegroundColor Yellow
Write-Host ""
Write-Host "🎯 Complete Workflow:" -ForegroundColor White
Write-Host "   Default values → Connection → Generate → Commit → Verify" -ForegroundColor Gray
Write-Host ""
Write-Host "🎯 Button State Management:" -ForegroundColor White
Write-Host "   Disabled → Enabled → Disabled → Enabled (cycle)" -ForegroundColor Gray
Write-Host ""
Write-Host "🎯 Data Persistence:" -ForegroundColor White
Write-Host "   Preview (rollback) → Commit (permanent) → Query (verification)" -ForegroundColor Gray
Write-Host ""
Write-Host "🎯 Schema Compatibility:" -ForegroundColor White
Write-Host "   Empty database → Sample tables → Or existing schema respect" -ForegroundColor Gray
Write-Host ""

Write-Host "🎉 MANUAL TESTING SETUP COMPLETE!" -ForegroundColor Yellow
Write-Host "==================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "✅ If all checklist items pass, the comprehensive fix is successful!" -ForegroundColor Green
Write-Host "   + Commit button logic works correctly" -ForegroundColor White
Write-Host "   + Default values are properly set" -ForegroundColor White
Write-Host "   + Query returns actual records after commit" -ForegroundColor White
Write-Host "   + Tool is flexible for different database schemas" -ForegroundColor White 