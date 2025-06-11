# PowerShell Script để chạy tất cả Integration Tests
# File: run-all-integration-tests.ps1

param(
    [string]$OutputDir = "TestResults/IT_$(Get-Date -Format 'yyyyMMddTHHmmss')",
    [switch]$Verbose = $false,
    [switch]$SkipDatabaseCheck = $false
)

Write-Host "=== CHẠY TOÀN BỘ INTEGRATION TESTS ===" -ForegroundColor Green
Write-Host "Thời gian bắt đầu: $(Get-Date)" -ForegroundColor Yellow

# Tạo thư mục output
$fullOutputPath = "SqlTestDataGenerator.Tests/$OutputDir"
if (!(Test-Path $fullOutputPath)) {
    New-Item -ItemType Directory -Path $fullOutputPath -Force | Out-Null
}

# Kiểm tra database connection (nếu không skip)
if (-not $SkipDatabaseCheck) {
    Write-Host "`n🔍 Kiểm tra kết nối database..." -ForegroundColor Cyan
    try {
        Push-Location "SqlTestDataGenerator.Tests"
        $dbTestResult = dotnet test --filter "FullyQualifiedName~DatabaseConnectionTest" --verbosity quiet 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "⚠️  CẢNH BÁO: Database connection test failed. Integration tests có thể bị fail." -ForegroundColor Red
            Write-Host "Bạn có thể dùng -SkipDatabaseCheck để bỏ qua kiểm tra này." -ForegroundColor Yellow
        } else {
            Write-Host "✅ Database connection OK" -ForegroundColor Green
        }
    } catch {
        Write-Host "⚠️  CẢNH BÁO: Không thể kiểm tra database connection" -ForegroundColor Red
    } finally {
        Pop-Location
    }
}

# Danh sách Integration Test files
$integrationTestFiles = @(
    "RealMySQLIntegrationTests.cs",
    "MySQLIntegrationDuplicateKeyTests.cs", 
    "ExecuteQueryWithTestDataAsyncDemoTests.cs",
    "CreateMySQLTablesTest.cs",
    "DatabaseConnectionTest.cs",
    "RecordCountStrictVerificationTests.cs",
    "SqlDateAddUTF8Tests.cs",
    "MySQLDateFunctionConverterTests.cs",
    "DuplicateKeyBugFixTests.cs",
    "AIIntegrationBasicTest.cs"
)

$totalTests = $integrationTestFiles.Count
$passedTests = 0
$failedTests = 0
$results = @()

Write-Host "Số lượng Integration Test files: $totalTests" -ForegroundColor Cyan

# Chạy từng test file
foreach ($testFile in $integrationTestFiles) {
    $testName = [System.IO.Path]::GetFileNameWithoutExtension($testFile)
    Write-Host "`n--- Đang chạy: $testFile ---" -ForegroundColor Yellow
    
    try {
        # Chuyển đến thư mục test project
        Push-Location "SqlTestDataGenerator.Tests"
        
        # Chạy test với output chi tiết
        $outputFile = "$OutputDir/IT_${testName}_Result.txt"
        $command = "dotnet test --filter `"FullyQualifiedName~$testName`" --verbosity normal --logger `"console;verbosity=detailed`""
        
        if ($Verbose) {
            Write-Host "Executing: $command" -ForegroundColor Gray
        }
        
        # Chạy với timeout để tránh hang
        $job = Start-Job -ScriptBlock {
            param($cmd, $output)
            Invoke-Expression "$cmd > `"$output`" 2>&1; `$LASTEXITCODE"
        } -ArgumentList $command, $outputFile
        
        # Đợi tối đa 300 giây (5 phút) cho mỗi test
        $testResult = Wait-Job $job -Timeout 300
        
        if ($testResult) {
            $exitCode = Receive-Job $job
            Remove-Job $job
            
            if ($exitCode -eq 0) {
                Write-Host "✅ PASSED: $testFile" -ForegroundColor Green
                $passedTests++
                $status = "PASSED"
            } else {
                Write-Host "❌ FAILED: $testFile (Exit Code: $exitCode)" -ForegroundColor Red
                $failedTests++
                $status = "FAILED"
            }
        } else {
            # Timeout
            Stop-Job $job
            Remove-Job $job
            Write-Host "⏰ TIMEOUT: $testFile (>5 phút)" -ForegroundColor Red
            $failedTests++
            $status = "TIMEOUT"
            $exitCode = -999
        }
        
        $results += [PSCustomObject]@{
            TestFile = $testFile
            Status = $status
            OutputFile = $outputFile
            ExitCode = $exitCode
        }
        
    } catch {
        Write-Host "❌ ERROR: $testFile - $($_.Exception.Message)" -ForegroundColor Red
        $failedTests++
        $results += [PSCustomObject]@{
            TestFile = $testFile
            Status = "ERROR"
            OutputFile = ""
            ExitCode = -1
            Error = $_.Exception.Message
        }
    } finally {
        Pop-Location
    }
}

# Tạo báo cáo tổng hợp
$reportFile = "SqlTestDataGenerator.Tests/$OutputDir/IT_Summary_Report.txt"
$summaryContent = @"
=== INTEGRATION TESTS EXECUTION SUMMARY ===
Thời gian thực hiện: $(Get-Date)
Tổng số test files: $totalTests
Tests đã pass: $passedTests
Tests bị fail: $failedTests
Tỷ lệ thành công: $([math]::Round(($passedTests / $totalTests) * 100, 2))%

=== CHI TIẾT KẾT QUẢ ===
"@

foreach ($result in $results) {
    $summaryContent += "`n$($result.Status.PadRight(8)) - $($result.TestFile)"
    if ($result.Error) {
        $summaryContent += " (Error: $($result.Error))"
    }
    if ($result.ExitCode -and $result.ExitCode -ne 0) {
        $summaryContent += " (Exit Code: $($result.ExitCode))"
    }
}

$summaryContent += @"

=== GHI CHÚ ===
- Integration tests cần database connection hợp lệ
- Một số tests có thể fail nếu database chưa setup đúng
- Check app.config cho database connection string
- Timeout được set là 5 phút cho mỗi test file
"@

$summaryContent | Out-File -FilePath $reportFile -Encoding UTF8

# Hiển thị kết quả cuối cùng
Write-Host "`n=== KẾT QUẢ CUỐI CÙNG ===" -ForegroundColor Cyan
Write-Host "✅ Tests thành công: $passedTests/$totalTests" -ForegroundColor Green
Write-Host "❌ Tests thất bại: $failedTests/$totalTests" -ForegroundColor Red
Write-Host "📁 Kết quả lưu tại: $fullOutputPath" -ForegroundColor Yellow
Write-Host "📄 Báo cáo tổng hợp: $reportFile" -ForegroundColor Yellow

if ($failedTests -gt 0) {
    Write-Host "`n⚠️  CÓ $failedTests INTEGRATION TESTS BỊ THẤT BẠI!" -ForegroundColor Red
    Write-Host "💡 Kiểm tra database connection và configuration" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "`n🎉 TẤT CẢ INTEGRATION TESTS ĐÃ PASS!" -ForegroundColor Green
    exit 0
} 