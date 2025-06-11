# PowerShell Script để chạy tất cả Unit Tests
# File: run-all-unit-tests.ps1

param(
    [string]$OutputDir = "TestResults/UT_$(Get-Date -Format 'yyyyMMddTHHmmss')",
    [switch]$Verbose = $false
)

Write-Host "=== CHẠY TOÀN BỘ UNIT TESTS ===" -ForegroundColor Green
Write-Host "Thời gian bắt đầu: $(Get-Date)" -ForegroundColor Yellow

# Tạo thư mục output
$fullOutputPath = "SqlTestDataGenerator.Tests/$OutputDir"
if (!(Test-Path $fullOutputPath)) {
    New-Item -ItemType Directory -Path $fullOutputPath -Force | Out-Null
}

# Danh sách Unit Test files
$unitTestFiles = @(
    "SqlQueryParserTests.cs",
    "SqlQueryParserV2Tests.cs", 
    "SqlQueryParserV3Tests.cs",
    "SqlQueryParserDebugTests.cs",
    "ConfigurationServiceTests.cs",
    "LoggerServiceTests.cs",
    "ConstraintAwareGenerationTests.cs",
    "ComplexDataGenerationTests.cs",
    "ComplexSqlGenerationTests.cs",
    "ComprehensiveConstraintExtractorTests.cs",
    "EnhancedConstraintExtractionTests.cs",
    "ExistsConstraintExtractionTests.cs",
    "MissingSqlPatternsTests.cs",
    "RecordCountVerificationTests.cs",
    "AssertFailBehaviorDemo.cs",
    "UnitTest1.cs"
)

$totalTests = $unitTestFiles.Count
$passedTests = 0
$failedTests = 0
$results = @()

Write-Host "Số lượng Unit Test files: $totalTests" -ForegroundColor Cyan

# Chạy từng test file
foreach ($testFile in $unitTestFiles) {
    $testName = [System.IO.Path]::GetFileNameWithoutExtension($testFile)
    Write-Host "`n--- Đang chạy: $testFile ---" -ForegroundColor Yellow
    
    try {
        # Chuyển đến thư mục test project
        Push-Location "SqlTestDataGenerator.Tests"
        
        # Chạy test với output chi tiết
        $outputFile = "$OutputDir/UT_${testName}_Result.txt"
        $command = "dotnet test --filter `"FullyQualifiedName~$testName`" --verbosity normal --logger `"console;verbosity=detailed`""
        
        if ($Verbose) {
            Write-Host "Executing: $command" -ForegroundColor Gray
        }
        
        $testResult = Invoke-Expression "$command > `"$outputFile`" 2>&1; `$LASTEXITCODE"
        
        if ($testResult -eq 0) {
            Write-Host "✅ PASSED: $testFile" -ForegroundColor Green
            $passedTests++
            $status = "PASSED"
        } else {
            Write-Host "❌ FAILED: $testFile" -ForegroundColor Red
            $failedTests++
            $status = "FAILED"
        }
        
        $results += [PSCustomObject]@{
            TestFile = $testFile
            Status = $status
            OutputFile = $outputFile
            ExitCode = $testResult
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
$reportFile = "SqlTestDataGenerator.Tests/$OutputDir/UT_Summary_Report.txt"
$summaryContent = @"
=== UNIT TESTS EXECUTION SUMMARY ===
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
}

$summaryContent | Out-File -FilePath $reportFile -Encoding UTF8

# Hiển thị kết quả cuối cùng
Write-Host "`n=== KẾT QUẢ CUỐI CÙNG ===" -ForegroundColor Cyan
Write-Host "✅ Tests thành công: $passedTests/$totalTests" -ForegroundColor Green
Write-Host "❌ Tests thất bại: $failedTests/$totalTests" -ForegroundColor Red
Write-Host "📁 Kết quả lưu tại: $fullOutputPath" -ForegroundColor Yellow
Write-Host "📄 Báo cáo tổng hợp: $reportFile" -ForegroundColor Yellow

if ($failedTests -gt 0) {
    Write-Host "`n⚠️  CÓ $failedTests TESTS BỊ THẤT BẠI!" -ForegroundColor Red
    exit 1
} else {
    Write-Host "`n🎉 TẤT CẢ UNIT TESTS ĐÃ PASS!" -ForegroundColor Green
    exit 0
} 