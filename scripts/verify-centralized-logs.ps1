#!/usr/bin/env pwsh
# ===================================================================
# CENTRALIZED LOGGING VERIFICATION SCRIPT
# ===================================================================
# Purpose: Verify all logs are unified in single location
# Date: $(Get-Date -Format "yyyy-MM-dd")

param(
    [switch]$ShowDetails,
    [switch]$CleanupOldLogs,
    [switch]$TestGeneration
)

Write-Host "🎯 CENTRALIZED LOGGING VERIFICATION" -ForegroundColor Cyan
Write-Host "Testing unified logs location and organization" -ForegroundColor Gray
Write-Host ""

# ===================================================================
# CONFIGURATION
# ===================================================================
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$LogsDir = Join-Path $ProjectRoot "logs"
$TestLogsDir = Join-Path $ProjectRoot "SqlTestDataGenerator.Tests\logs"
$UIBinLogs = Join-Path $ProjectRoot "SqlTestDataGenerator.UI\bin\Debug\net8.0\logs"

Write-Host "📁 CHECKING LOG DIRECTORIES" -ForegroundColor Yellow
Write-Host "Root logs directory: $LogsDir"
Write-Host "Test logs directory: $TestLogsDir"
Write-Host "UI bin logs directory: $UIBinLogs"
Write-Host ""

# ===================================================================
# VERIFY CENTRALIZED LOGS
# ===================================================================
Write-Host "🔍 VERIFYING LOGS STRUCTURE" -ForegroundColor Green

# Check main logs directory
if (Test-Path $LogsDir) {
    $mainLogFiles = Get-ChildItem -Path $LogsDir -Filter "*.log" -ErrorAction SilentlyContinue
    Write-Host "✅ Main logs directory exists: $($mainLogFiles.Count) files"
    
    if ($ShowDetails -and $mainLogFiles.Count -gt 0) {
        Write-Host "   📄 Log files in main directory:"
        foreach ($file in $mainLogFiles) {
            $sizeMB = [Math]::Round($file.Length / 1MB, 2)
            Write-Host "   - $($file.Name) ($sizeMB MB, Modified: $($file.LastWriteTime.ToString('yyyy-MM-dd HH:mm')))"
        }
    }
} else {
    Write-Host "❌ Main logs directory does not exist!" -ForegroundColor Red
}

# Check for old test logs directory
if (Test-Path $TestLogsDir) {
    $testLogFiles = Get-ChildItem -Path $TestLogsDir -Filter "*.log" -ErrorAction SilentlyContinue
    if ($testLogFiles.Count -gt 0) {
        Write-Host "⚠️  Old test logs directory still has files: $($testLogFiles.Count)" -ForegroundColor Yellow
        
        if ($ShowDetails) {
            Write-Host "   📄 Old test log files:"
            foreach ($file in $testLogFiles) {
                $sizeMB = [Math]::Round($file.Length / 1MB, 2)
                Write-Host "   - $($file.Name) ($sizeMB MB)"
            }
        }
        
        if ($CleanupOldLogs) {
            Write-Host "🧹 Cleaning up old test logs..." -ForegroundColor Magenta
            try {
                Move-Item -Path (Join-Path $TestLogsDir "*.log") -Destination $LogsDir -Force
                Write-Host "✅ Moved old test logs to centralized location"
            } catch {
                Write-Host "❌ Failed to move old logs: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "✅ Old test logs directory is clean"
    }
} else {
    Write-Host "✅ No old test logs directory found"
}

# Check for UI bin logs (should not exist)
if (Test-Path $UIBinLogs) {
    $uiBinLogFiles = Get-ChildItem -Path $UIBinLogs -Filter "*.log" -ErrorAction SilentlyContinue
    if ($uiBinLogFiles.Count -gt 0) {
        Write-Host "⚠️  UI bin logs directory has files: $($uiBinLogFiles.Count)" -ForegroundColor Yellow
        
        if ($CleanupOldLogs) {
            Write-Host "🧹 Cleaning up UI bin logs..." -ForegroundColor Magenta
            try {
                Move-Item -Path (Join-Path $UIBinLogs "*.log") -Destination $LogsDir -Force
                Write-Host "✅ Moved UI bin logs to centralized location"
            } catch {
                Write-Host "❌ Failed to move UI bin logs: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "✅ UI bin logs directory is clean"
    }
} else {
    Write-Host "✅ No UI bin logs directory found"
}

Write-Host ""

# ===================================================================
# LOGS CATEGORIZATION ANALYSIS
# ===================================================================
Write-Host "📊 LOGS CATEGORIZATION ANALYSIS" -ForegroundColor Green

if (Test-Path $LogsDir) {
    $allLogs = Get-ChildItem -Path $LogsDir -Filter "*.log"
    
    $categories = @{
        "UI" = @($allLogs | Where-Object { $_.Name -like "*app*" })
        "Engine" = @($allLogs | Where-Object { $_.Name -like "*engine*" })
        "Test" = @($allLogs | Where-Object { $_.Name -like "*test*" })
        "AI" = @($allLogs | Where-Object { $_.Name -like "*ai*" })
        "Other" = @($allLogs | Where-Object { $_.Name -notlike "*app*" -and $_.Name -notlike "*engine*" -and $_.Name -notlike "*test*" -and $_.Name -notlike "*ai*" })
    }
    
    $totalSize = ($allLogs | Measure-Object -Property Length -Sum).Sum / 1MB
    
    Write-Host "📈 Total log files: $($allLogs.Count)"
    Write-Host "📈 Total size: $([Math]::Round($totalSize, 2)) MB"
    Write-Host ""
    
    foreach ($category in $categories.Keys) {
        $files = $categories[$category]
        if ($files.Count -gt 0) {
            $categorySize = ($files | Measure-Object -Property Length -Sum).Sum / 1MB
            Write-Host "   $category`: $($files.Count) files ($([Math]::Round($categorySize, 2)) MB)"
        } else {
            Write-Host "   $category`: 0 files"
        }
    }
}

Write-Host ""

# ===================================================================
# TEST LOG GENERATION
# ===================================================================
if ($TestGeneration) {
    Write-Host "🧪 TESTING LOG GENERATION" -ForegroundColor Magenta
    Write-Host "Running quick test to verify logs are written to centralized location..."
    
    try {
        # Change to project root
        Push-Location $ProjectRoot
        
        # Build project
        Write-Host "🔨 Building project..."
        dotnet build --configuration Debug --verbosity quiet
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Build successful"
            
            # Run a simple test
            Write-Host "🔬 Running simple test..."
            dotnet test SqlTestDataGenerator.Tests --filter "TestMethod=DatabaseConnectionTest" --verbosity quiet --no-build
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Test completed"
                
                # Check if new logs were created
                Start-Sleep -Seconds 2
                $newLogs = Get-ChildItem -Path $LogsDir -Filter "*.log" | Where-Object { $_.LastWriteTime -gt (Get-Date).AddMinutes(-5) }
                
                if ($newLogs.Count -gt 0) {
                    Write-Host "✅ New logs detected in centralized location: $($newLogs.Count) files"
                    foreach ($log in $newLogs) {
                        Write-Host "   - $($log.Name) (Modified: $($log.LastWriteTime.ToString('HH:mm:ss')))"
                    }
                } else {
                    Write-Host "⚠️  No new logs detected (may be normal for quick tests)" -ForegroundColor Yellow
                }
            } else {
                Write-Host "❌ Test failed" -ForegroundColor Red
            }
        } else {
            Write-Host "❌ Build failed" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Test generation failed: $($_.Exception.Message)" -ForegroundColor Red
    } finally {
        Pop-Location
    }
}

# ===================================================================
# SUMMARY & RECOMMENDATIONS
# ===================================================================
Write-Host ""
Write-Host "📋 VERIFICATION SUMMARY" -ForegroundColor Cyan

$issues = @()
$successes = @()

if (Test-Path $LogsDir) {
    $mainLogs = (Get-ChildItem -Path $LogsDir -Filter "*.log" -ErrorAction SilentlyContinue).Count
    $successes += "✅ Centralized logs directory exists with $mainLogs files"
} else {
    $issues += "❌ Main logs directory missing"
}

if (Test-Path $TestLogsDir) {
    $oldTestLogs = (Get-ChildItem -Path $TestLogsDir -Filter "*.log" -ErrorAction SilentlyContinue).Count
    if ($oldTestLogs -gt 0) {
        $issues += "⚠️  $oldTestLogs old test logs need migration"
    } else {
        $successes += "✅ Old test logs directory is clean"
    }
}

if (Test-Path $UIBinLogs) {
    $oldUILogs = (Get-ChildItem -Path $UIBinLogs -Filter "*.log" -ErrorAction SilentlyContinue).Count
    if ($oldUILogs -gt 0) {
        $issues += "⚠️  $oldUILogs old UI bin logs need migration"
    }
}

Write-Host ""
foreach ($success in $successes) {
    Write-Host $success -ForegroundColor Green
}

foreach ($issue in $issues) {
    Write-Host $issue -ForegroundColor Yellow
}

if ($issues.Count -eq 0) {
    Write-Host ""
    Write-Host "🎉 CENTRALIZED LOGGING SUCCESSFULLY CONFIGURED!" -ForegroundColor Green
    Write-Host "All logs are now unified in: $LogsDir" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "🔧 RECOMMENDATIONS:" -ForegroundColor Yellow
    Write-Host "   Run with -CleanupOldLogs to migrate scattered logs"
    Write-Host "   Run with -TestGeneration to verify new log creation"
    Write-Host "   Run with -ShowDetails for detailed file analysis"
}

Write-Host "" 