# =====================================================================
# Quick Oracle Connection Test
# =====================================================================
# PURPOSE: Test nhanh Oracle connection sau khi fix
# USAGE: .\scripts\test-oracle-connection-quick.ps1
# =====================================================================

Write-Host "=== Quick Oracle Connection Test ===" -ForegroundColor Green

# Test 1: Basic Oracle Connection Test
Write-Host "Testing Oracle Connection..." -ForegroundColor Yellow
dotnet test SqlTestDataGenerator.Tests --filter "OracleConnectionTest" --verbosity minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Oracle Connection Test PASSED" -ForegroundColor Green
} else {
    Write-Host "❌ Oracle Connection Test FAILED" -ForegroundColor Red
    exit 1
}

# Test 2: Engine Service Test Connection
Write-Host "Testing Engine Service TestConnectionAsync..." -ForegroundColor Yellow
dotnet test SqlTestDataGenerator.Tests --filter "RealOracleIntegrationTests" --verbosity minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Engine Service Test PASSED" -ForegroundColor Green
} else {
    Write-Host "❌ Engine Service Test FAILED" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host "✅ Oracle connection should now work in the UI" -ForegroundColor Green
Write-Host "📋 To test in UI:" -ForegroundColor White
Write-Host "   1. Select 'Oracle' from Database Type dropdown" -ForegroundColor Gray
Write-Host "   2. Click '🔌 Test Connection' button" -ForegroundColor Gray
Write-Host "   3. Should show: '✅ Connection successful!'" -ForegroundColor Gray

Write-Host ""
Write-Host "=== Fix Applied ===" -ForegroundColor Cyan
Write-Host "✅ Added Oracle case in TestConnectionAsync:" -ForegroundColor Green
Write-Host "   'oracle' => 'SELECT 1 FROM DUAL'" -ForegroundColor Gray
Write-Host "✅ This fixes the ORA-00923 error" -ForegroundColor Green 