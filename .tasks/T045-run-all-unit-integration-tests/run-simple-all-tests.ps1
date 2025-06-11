# Script đơn giản để chạy tất cả tests cùng lúc
# File: run-simple-all-tests.ps1

param(
    [string]$OutputDir = "TestResults/SIMPLE_$(Get-Date -Format 'yyyyMMddTHHmmss')",
    [switch]$Verbose = $false
)

Write-Host "🚀 CHẠY TẤT CẢ TESTS - SqlTestDataGenerator" -ForegroundColor Green
Write-Host "Thời gian bắt đầu: $(Get-Date)" -ForegroundColor Yellow

# Tạo thư mục output
$fullOutputPath = "SqlTestDataGenerator.Tests/$OutputDir"
if (!(Test-Path $fullOutputPath)) {
    New-Item -ItemType Directory -Path $fullOutputPath -Force | Out-Null
}

# Chuyển đến thư mục test project
Push-Location "SqlTestDataGenerator.Tests"

try {
    Write-Host "`n📊 Đang chạy tất cả tests..." -ForegroundColor Cyan
    
    # Build project trước
    Write-Host "🔨 Building test project..." -ForegroundColor Yellow
    dotnet build --configuration Release --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Build successful" -ForegroundColor Green
    
    # Chạy tất cả tests
    $outputFile = "$OutputDir/All_Tests_Result.txt"
    $command = "dotnet test --verbosity normal --logger `"console;verbosity=detailed`" --logger `"trx;LogFileName=all_tests.trx`""
    
    if ($Verbose) {
        Write-Host "Command: $command" -ForegroundColor Gray
    }
    
    Write-Host "`n🧪 Executing all tests..." -ForegroundColor Yellow
    $startTime = Get-Date
    
    $testResult = Invoke-Expression "$command > `"$outputFile`" 2>&1; `$LASTEXITCODE"
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    # Đọc output để phân tích kết quả
    $output = Get-Content $outputFile -Raw
    
    # Tìm số liệu từ output
    $passedPattern = "Passed!\s*-\s*Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)"
    $failedPattern = "Failed!\s*-\s*Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)"
    
    $passed = 0
    $failed = 0
    $skipped = 0
    $total = 0
    
    if ($output -match $passedPattern) {
        $failed = [int]$matches[1]
        $passed = [int]$matches[2]
        $skipped = [int]$matches[3]
        $total = [int]$matches[4]
    } elseif ($output -match $failedPattern) {
        $failed = [int]$matches[1]
        $passed = [int]$matches[2]
        $skipped = [int]$matches[3]
        $total = [int]$matches[4]
    } else {
        # Fallback: tìm pattern khác
        if ($output -match "(\d+) passed") {
            $passed = [int]$matches[1]
        }
        if ($output -match "(\d+) failed") {
            $failed = [int]$matches[1]
        }
        if ($output -match "Total tests: (\d+)") {
            $total = [int]$matches[1]
        }
    }
    
    if ($total -eq 0) {
        $total = $passed + $failed + $skipped
    }
    
    # Tạo báo cáo
    $reportFile = "$OutputDir/Simple_Test_Report.txt"
    $reportContent = @"
════════════════════════════════════════════════════════
📋 BÁO CÁO TEST TOÀN BỘ - SqlTestDataGenerator
════════════════════════════════════════════════════════
Thời gian bắt đầu: $startTime
Thời gian kết thúc: $endTime
Thời gian thực hiện: $($duration.ToString("hh\:mm\:ss"))

🎯 KẾT QUẢ TỔNG QUÁT
Exit Code: $testResult
Tổng số tests: $total
Tests đã pass: $passed
Tests bị fail: $failed
Tests bị skip: $skipped
Tỷ lệ thành công: $([math]::Round(($passed / [math]::Max($total, 1)) * 100, 2))%

📁 Output file: $outputFile
📄 TRX file: TestResults/all_tests.trx (nếu có)

════════════════════════════════════════════════════════
"@
    
    $reportContent | Out-File -FilePath $reportFile -Encoding UTF8
    
    # Hiển thị kết quả
    Write-Host "`n════════════════════════════════════════════════════════" -ForegroundColor Blue
    Write-Host "🏁 KẾT QUẢ CUỐI CÙNG" -ForegroundColor Blue
    Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Blue
    Write-Host "⏱️  Thời gian thực hiện: $($duration.ToString("hh\:mm\:ss"))" -ForegroundColor Yellow
    Write-Host "📊 Tổng kết: $passed/$total tests passed ($([math]::Round(($passed / [math]::Max($total, 1)) * 100, 2))%)" -ForegroundColor Cyan
    
    if ($failed -gt 0) {
        Write-Host "❌ Failed: $failed" -ForegroundColor Red
    }
    if ($skipped -gt 0) {
        Write-Host "⏭️  Skipped: $skipped" -ForegroundColor Yellow
    }
    
    Write-Host "📁 Kết quả chi tiết: $fullOutputPath" -ForegroundColor Yellow
    Write-Host "📄 Báo cáo: $reportFile" -ForegroundColor Yellow
    
    if ($testResult -eq 0) {
        Write-Host "`n🎉 TẤT CẢ TESTS THÀNH CÔNG!" -ForegroundColor Green
    } else {
        Write-Host "`n⚠️  CÓ TESTS BỊ THẤT BẠI!" -ForegroundColor Red
    }
    
    exit $testResult
    
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
} 