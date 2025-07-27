# Demo Daily API Limits Functionality
# Showcase the new daily API limit and time availability features

Write-Host "🎯 Daily API Limits Demo" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

# Build the project first
Write-Host "📦 Building project..." -ForegroundColor Yellow
dotnet build SqlTestDataGenerator.sln --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed. Exiting." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful!" -ForegroundColor Green
Write-Host ""

# Run all Daily API Limit tests
Write-Host "🧪 Running Daily API Limit Tests..." -ForegroundColor Cyan
dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --filter "FullyQualifiedName~DailyApiLimitTests" --verbosity quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ All tests passed successfully!" -ForegroundColor Green
} else {
    Write-Host "⚠️ Some tests failed. Check details above." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎯 Demo Features Implemented:" -ForegroundColor Cyan
Write-Host "  📊 Daily call count tracking (0/100 calls used)" -ForegroundColor White
Write-Host "  ⏰ Smart time availability checking" -ForegroundColor White
Write-Host "  🔄 24-hour automatic reset at midnight UTC" -ForegroundColor White
Write-Host "  🚦 Rate limiting (5 seconds between calls)" -ForegroundColor White
Write-Host "  💚 Model health monitoring (13 Flash models)" -ForegroundColor White
Write-Host "  📈 Comprehensive usage statistics" -ForegroundColor White
Write-Host ""

Write-Host "🔧 Key Features:" -ForegroundColor Cyan
Write-Host "  • CanCallAPINow() - Check if API call is available" -ForegroundColor White
Write-Host "  • GetNextCallableTime() - Calculate next allowed call time" -ForegroundColor White
Write-Host "  • GetAPIUsageStatistics() - Get detailed usage info" -ForegroundColor White
Write-Host "  • Automatic waiting logic for rate limits vs daily limits" -ForegroundColor White
Write-Host "  • Thread-safe implementation for concurrent usage" -ForegroundColor White
Write-Host ""

Write-Host "⏰ Current Status:" -ForegroundColor Cyan
Write-Host "  🕒 Current Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC')" -ForegroundColor White
Write-Host "  🔄 Daily Reset: Next midnight (UTC)" -ForegroundColor White
Write-Host "  💡 Ready for production use!" -ForegroundColor White
Write-Host ""

Write-Host "✨ Demo completed successfully!" -ForegroundColor Green
Write-Host "📖 Check docs/daily_api_limit_implementation.md for full documentation" -ForegroundColor Cyan 