#!/usr/bin/env pwsh

Write-Host "🔍 Debugging Table Insert Issues..." -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "🎯 DEBUGGING OBJECTIVE:" -ForegroundColor Yellow
Write-Host "========================" -ForegroundColor Yellow
Write-Host ""
Write-Host "User reported: 'table roles trong DB không có dữ liệu khi chạy file SQL thì có câu lệnh insert rồi mà'" -ForegroundColor White
Write-Host "Translation: 'roles table in DB has no data even though SQL file has insert commands'" -ForegroundColor Gray
Write-Host ""
Write-Host "Possible causes:" -ForegroundColor White
Write-Host "• INSERT IGNORE skipping due to duplicate keys" -ForegroundColor Gray
Write-Host "• Foreign key constraint failures" -ForegroundColor Gray
Write-Host "• Transaction rollbacks" -ForegroundColor Gray
Write-Host "• Silent execution failures" -ForegroundColor Gray
Write-Host ""

Write-Host "🔧 FIXES IMPLEMENTED:" -ForegroundColor Green
Write-Host "=====================" -ForegroundColor Green
Write-Host ""
Write-Host "✅ Enhanced Error Detection:" -ForegroundColor Green
Write-Host "   • Added row count logging for each INSERT" -ForegroundColor Gray
Write-Host "   • Check existing records if INSERT returns 0" -ForegroundColor Gray
Write-Host "   • Throw exception if table empty and INSERT failed" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ Changed INSERT Strategy:" -ForegroundColor Green
Write-Host "   • Changed roles INSERT IGNORE to REPLACE INTO" -ForegroundColor Gray
Write-Host "   • REPLACE ensures data is inserted even if exists" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ Added Final Verification:" -ForegroundColor Green
Write-Host "   • Count records in all tables after creation" -ForegroundColor Gray
Write-Host "   • Display record counts in status message" -ForegroundColor Gray
Write-Host "   • Console logging for debugging" -ForegroundColor Gray
Write-Host ""

Write-Host "🚀 TESTING ENHANCED TABLE CREATION..." -ForegroundColor Yellow
Write-Host "======================================" -ForegroundColor Yellow
Write-Host ""

try {
    # Build the application
    Write-Host "🔧 Building application..." -ForegroundColor White
    $buildResult = dotnet build SqlTestDataGenerator.UI --verbosity minimal --no-restore 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        Write-Host $buildResult -ForegroundColor Gray
        exit 1
    }
    
    Write-Host "✅ Build successful!" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "📋 DEBUGGING CHECKLIST:" -ForegroundColor Yellow
    Write-Host "========================" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 1: 🔗 TEST CONNECTION" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "• Click 'Test Connection' button" -ForegroundColor Yellow
    Write-Host "• Watch Console output for detailed logging:" -ForegroundColor Yellow
    Write-Host "  - 'Companies insert: X rows affected'" -ForegroundColor Gray
    Write-Host "  - 'Roles insert: X rows affected'" -ForegroundColor Gray
    Write-Host "  - 'Users insert: X rows affected'" -ForegroundColor Gray
    Write-Host "  - 'User_roles insert: X rows affected'" -ForegroundColor Gray
    Write-Host "  - 'Final count - roles: X records'" -ForegroundColor Gray
    Write-Host "• Status should show record counts for all tables" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 2: 🔍 CHECK STATUS MESSAGE" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "• Status should show: '✅ MySQL tables created successfully: companies: X records, roles: X records, users: X records, user_roles: X records'" -ForegroundColor Yellow
    Write-Host "• If roles shows 0 records, this indicates the problem" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 3: 🚨 ERROR SCENARIOS TO WATCH" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "• Console: 'Roles insert: 0 rows affected'" -ForegroundColor Red
    Write-Host "• Console: 'Existing roles in table: 0'" -ForegroundColor Red
    Write-Host "• Exception: 'Failed to insert roles and table is empty'" -ForegroundColor Red
    Write-Host "• These indicate actual INSERT failure (not just duplicates)" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "STEP 4: 🔄 SUCCESS SCENARIOS" -ForegroundColor White
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "• Console: 'Roles insert: 10 rows affected' (first time)" -ForegroundColor Green
    Write-Host "• Console: 'Roles insert: 10 rows affected' (with REPLACE)" -ForegroundColor Green
    Write-Host "• Console: 'Final count - roles: 10 records'" -ForegroundColor Green
    Write-Host "• Status: 'roles: 10 records' in summary" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "🎯 EXPECTED IMPROVEMENTS:" -ForegroundColor Yellow
    Write-Host "==========================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "✅ TRANSPARENCY: See exactly which inserts succeed/fail" -ForegroundColor Green
    Write-Host "✅ ERROR DETECTION: Know immediately if roles table is empty" -ForegroundColor Green
    Write-Host "✅ FORCED INSERTION: REPLACE INTO ensures roles are always inserted" -ForegroundColor Green
    Write-Host "✅ VERIFICATION: Final counts confirm data actually exists" -ForegroundColor Green
    Write-Host ""
    
    # Launch the application
    Write-Host "🚀 Launching application for debugging..." -ForegroundColor White
    Start-Process -FilePath "SqlTestDataGenerator.UI\bin\Debug\net8.0-windows\SqlTestDataGenerator.UI.exe" -WorkingDirectory "."
    
    Write-Host "📱 Application launched!" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "🔍 DEBUGGING INSTRUCTIONS:" -ForegroundColor Yellow
    Write-Host "===========================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. 📺 Open Console window (if not visible)" -ForegroundColor White
    Write-Host "2. 🔗 Click 'Test Connection' to trigger table creation" -ForegroundColor White
    Write-Host "3. 👀 Watch Console for detailed insert logging" -ForegroundColor White
    Write-Host "4. 📊 Check Status message for record counts" -ForegroundColor White
    Write-Host "5. 🚨 Note any errors or 0 row counts" -ForegroundColor White
    Write-Host "6. 🔄 Try Test Connection multiple times to see REPLACE behavior" -ForegroundColor White
    Write-Host ""
    
    Write-Host "⚠️  WHAT TO REPORT:" -ForegroundColor Red
    Write-Host "===================" -ForegroundColor Red
    Write-Host ""
    Write-Host "🔴 If Console shows 'Roles insert: 0 rows affected'" -ForegroundColor Red
    Write-Host "🔴 If Status shows 'roles: 0 records'" -ForegroundColor Red
    Write-Host "🔴 If you see exception about empty roles table" -ForegroundColor Red
    Write-Host "🔴 Any other unexpected behavior or errors" -ForegroundColor Red
    Write-Host ""
    
    Write-Host "✅ GOOD SIGNS:" -ForegroundColor Green
    Write-Host "===============" -ForegroundColor Green
    Write-Host ""
    Write-Host "🟢 Console shows 'Roles insert: 10 rows affected'" -ForegroundColor Green
    Write-Host "🟢 Console shows 'Final count - roles: 10 records'" -ForegroundColor Green
    Write-Host "🟢 Status shows 'roles: 10 records'" -ForegroundColor Green
    Write-Host "🟢 No exceptions thrown" -ForegroundColor Green
    Write-Host ""
    
} catch {
    Write-Host "❌ Error during debugging setup:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Gray
}

Write-Host ""
Write-Host "📊 DEBUGGING SUMMARY:" -ForegroundColor Yellow
Write-Host "=====================" -ForegroundColor Yellow
Write-Host ""
Write-Host "🎯 Purpose: Find root cause of empty roles table" -ForegroundColor White
Write-Host "🔧 Tools: Enhanced logging, error detection, verification" -ForegroundColor White
Write-Host "📈 Outcome: Detailed visibility into INSERT operations" -ForegroundColor White
Write-Host ""
Write-Host "🎉 DEBUGGING SETUP COMPLETE!" -ForegroundColor Yellow
Write-Host "============================" -ForegroundColor Yellow
Write-Host ""
Write-Host "The enhanced logging will help identify exactly why roles table is empty." -ForegroundColor White
Write-Host "Please test and report the Console output and Status messages." -ForegroundColor Gray 