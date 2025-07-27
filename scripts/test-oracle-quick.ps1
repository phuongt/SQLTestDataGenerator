# Quick Oracle Test with Model Rotation
# Kiểm tra nhanh xem Oracle test có còn bị timeout không

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   QUICK ORACLE TEST (MODEL ROTATION)" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

$StartTime = Get-Date
$Timeout = 300 # 5 minutes timeout

Write-Host "🚀 Starting Oracle test with model rotation..." -ForegroundColor Green
Write-Host "⏱️  Timeout: $Timeout seconds" -ForegroundColor Yellow
Write-Host ""

try {
    # Run Oracle test with timeout
    $Process = Start-Process -FilePath "dotnet" -ArgumentList "test", "../SqlTestDataGenerator.Tests/OracleComplexQueryPhương1989Tests.cs", "--logger", "console;verbosity=normal" -PassThru -NoNewWindow
    
    # Wait for completion or timeout
    $Completed = $Process.WaitForExit($Timeout * 1000)
    
    $EndTime = Get-Date
    $Duration = $EndTime - $StartTime
    
    if ($Completed) {
        if ($Process.ExitCode -eq 0) {
            Write-Host "✅ Oracle test PASSED!" -ForegroundColor Green
            Write-Host "📊 Duration: $($Duration.ToString('mm\:ss'))" -ForegroundColor Green
            Write-Host "🎯 Model rotation working correctly!" -ForegroundColor Green
        } else {
            Write-Host "❌ Oracle test FAILED!" -ForegroundColor Red
            Write-Host "📊 Duration: $($Duration.ToString('mm\:ss'))" -ForegroundColor Red
            Write-Host "🔍 Exit Code: $($Process.ExitCode)" -ForegroundColor Red
        }
    } else {
        Write-Host "⏰ Oracle test TIMEOUT after $Timeout seconds!" -ForegroundColor Red
        Write-Host "💀 Process killed due to timeout" -ForegroundColor Red
        
        # Kill the process if it's still running
        if (!$Process.HasExited) {
            $Process.Kill()
        }
    }
}
catch {
    Write-Host "❌ Error running Oracle test: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   TEST COMPLETED" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan 