# Test Model Health Persistence
# Kiểm tra việc lưu và đọc model health data từ file

param(
    [switch]$Verbose = $false,
    [switch]$CleanStart = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   TEST MODEL HEALTH PERSISTENCE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Initialize variables
$StartTime = Get-Date
$LogFile = "logs/test-model-health-persistence-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').log"

# Ensure logs directory exists
if (!(Test-Path "logs")) {
    New-Item -ItemType Directory -Path "logs" -Force | Out-Null
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogEntry = "[$Timestamp] [$Level] $Message"
    
    # Console output with colors
    switch ($Level) {
        "SUCCESS" { Write-Host $LogEntry -ForegroundColor Green }
        "ERROR" { Write-Host $LogEntry -ForegroundColor Red }
        "WARNING" { Write-Host $LogEntry -ForegroundColor Yellow }
        "DEBUG" { 
            if ($Verbose) { 
                Write-Host $LogEntry -ForegroundColor Gray 
            }
        }
        default { Write-Host $LogEntry }
    }
    
    # Always write to log file
    Add-Content -Path $LogFile -Value $LogEntry
}

# Clean start if requested
if ($CleanStart) {
    Write-Log "🧹 Cleaning up previous test data..." "INFO"
    $DataDir = "data"
    $HealthFile = "$DataDir/model-health.json"
    
    if (Test-Path $HealthFile) {
        Remove-Item $HealthFile -Force
        Write-Log "✅ Removed existing health file: $HealthFile" "SUCCESS"
    }
    
    if (Test-Path $DataDir) {
        Remove-Item $DataDir -Recurse -Force
        Write-Log "✅ Removed data directory: $DataDir" "SUCCESS"
    }
}

# Navigate to test directory
$OriginalLocation = Get-Location
Push-Location "SqlTestDataGenerator.Tests"

try {
    Write-Log "🚀 Starting Model Health Persistence Test" "INFO"
    Write-Log "📁 Test Directory: $(Get-Location)" "INFO"
    Write-Log "📄 Log File: $LogFile" "INFO"
    
    # Build the project first
    Write-Log "🔨 Building test project..." "INFO"
    $BuildResult = dotnet build --verbosity normal 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Log "❌ Build failed with exit code: $LASTEXITCODE" "ERROR"
        Write-Log "Build failure details: $BuildResult" "ERROR"
        exit 1
    }
    Write-Log "✅ Build successful" "SUCCESS"
    
    # Test 1: Check if health file is created
    Write-Log "🧪 Test 1: Check health file creation" "INFO"
    
    $TestCommand = "dotnet test --filter `"OracleComplexQueryPhuong1989Tests`" --verbosity normal 2>&1"
    Write-Log "⚡ Running: $TestCommand" "DEBUG"
    
    $TestResult = Invoke-Expression $TestCommand
    $TestExitCode = $LASTEXITCODE
    
    Write-Log "Test Exit Code: $TestExitCode" "DEBUG"
    Write-Log "Test Output: $TestResult" "DEBUG"
    
    # Check if health file was created
    $DataDir = "data"
    $HealthFile = "$DataDir/model-health.json"
    
    if (Test-Path $HealthFile) {
        Write-Log "✅ Health file created: $HealthFile" "SUCCESS"
        
        # Read and display health file content
        $HealthContent = Get-Content $HealthFile -Raw
        Write-Log "📄 Health file content:" "INFO"
        Write-Log $HealthContent "DEBUG"
        
        # Parse JSON to check structure
        try {
            $HealthData = $HealthContent | ConvertFrom-Json
            $ModelCount = ($HealthData.PSObject.Properties | Measure-Object).Count
            Write-Log "📊 Health data contains $ModelCount models" "INFO"
            
            # Check for specific models
            $Models = $HealthData.PSObject.Properties.Name
            Write-Log "🔍 Models in health file: $($Models -join ', ')" "INFO"
            
            # Check for recovery times
            $ModelsWithRecovery = @()
            foreach ($Model in $Models) {
                $ModelData = $HealthData.$Model
                if ($ModelData.RecoveryTime) {
                    $ModelsWithRecovery += "$Model (recovery: $($ModelData.RecoveryTime))"
                }
            }
            
            if ($ModelsWithRecovery.Count -gt 0) {
                Write-Log "⏰ Models with recovery times: $($ModelsWithRecovery -join ', ')" "INFO"
            } else {
                Write-Log "✅ All models are healthy (no recovery times)" "SUCCESS"
            }
            
        } catch {
            Write-Log "⚠️ Failed to parse health file JSON: $($_.Exception.Message)" "WARNING"
        }
    } else {
        Write-Log "❌ Health file not found: $HealthFile" "ERROR"
    }
    
    # Test 2: Check persistence across restarts
    Write-Log "🧪 Test 2: Check persistence across restarts" "INFO"
    
    if (Test-Path $HealthFile) {
        Write-Log "📂 Health file exists, testing reload..." "INFO"
        
        # Run another test to see if data persists
        $TestCommand2 = "dotnet test --filter `"OracleComplexQueryPhuong1989Tests`" --verbosity normal 2>&1"
        Write-Log "⚡ Running second test: $TestCommand2" "DEBUG"
        
        $TestResult2 = Invoke-Expression $TestCommand2
        $TestExitCode2 = $LASTEXITCODE
        
        Write-Log "Second Test Exit Code: $TestExitCode2" "DEBUG"
        
        # Check if health file was updated
        if (Test-Path $HealthFile) {
            $HealthContent2 = Get-Content $HealthFile -Raw
            Write-Log "✅ Health file updated after second test" "SUCCESS"
            
            # Compare file sizes to see if data changed
            $OriginalSize = (Get-Item $HealthFile).Length
            Write-Log "📊 Health file size: $OriginalSize bytes" "INFO"
            
            # Check for any recovery times in the updated file
            try {
                $HealthData2 = $HealthContent2 | ConvertFrom-Json
                $ModelsWithRecovery2 = @()
                foreach ($Model in $HealthData2.PSObject.Properties.Name) {
                    $ModelData = $HealthData2.$Model
                    if ($ModelData.RecoveryTime) {
                        $ModelsWithRecovery2 += "$Model (recovery: $($ModelData.RecoveryTime))"
                    }
                }
                
                if ($ModelsWithRecovery2.Count -gt 0) {
                    Write-Log "⏰ Updated models with recovery times: $($ModelsWithRecovery2 -join ', ')" "INFO"
                }
            } catch {
                Write-Log "⚠️ Failed to parse updated health file: $($_.Exception.Message)" "WARNING"
            }
        }
    }
    
    # Test 3: Check file location and permissions
    Write-Log "🧪 Test 3: Check file location and permissions" "INFO"
    
    if (Test-Path $HealthFile) {
        $FileInfo = Get-Item $HealthFile
        Write-Log "📁 File location: $($FileInfo.FullName)" "INFO"
        Write-Log "📅 Created: $($FileInfo.CreationTime)" "INFO"
        Write-Log "📅 Modified: $($FileInfo.LastWriteTime)" "INFO"
        Write-Log "📊 Size: $($FileInfo.Length) bytes" "INFO"
        
        # Check if file is readable and writable
        try {
            $TestContent = Get-Content $HealthFile -Raw
            Write-Log "✅ File is readable" "SUCCESS"
        } catch {
            Write-Log "❌ File is not readable: $($_.Exception.Message)" "ERROR"
        }
        
        try {
            $TestContent | Set-Content $HealthFile -NoNewline
            Write-Log "✅ File is writable" "SUCCESS"
        } catch {
            Write-Log "❌ File is not writable: $($_.Exception.Message)" "ERROR"
        }
    }
    
    # Test 4: Check data directory structure
    Write-Log "🧪 Test 4: Check data directory structure" "INFO"
    
    if (Test-Path $DataDir) {
        $DataDirInfo = Get-Item $DataDir
        Write-Log "📁 Data directory: $($DataDirInfo.FullName)" "INFO"
        Write-Log "📅 Created: $($DataDirInfo.CreationTime)" "INFO"
        
        $FilesInDataDir = Get-ChildItem $DataDir -File
        Write-Log "📄 Files in data directory: $($FilesInDataDir.Count)" "INFO"
        
        foreach ($File in $FilesInDataDir) {
            Write-Log "   - $($File.Name) ($($File.Length) bytes)" "DEBUG"
        }
    } else {
        Write-Log "❌ Data directory not found: $DataDir" "ERROR"
    }
    
}
finally {
    Pop-Location
    Write-Log "Returned to original location: $(Get-Location)" "DEBUG"
}

# Generate Summary Report
$EndTime = Get-Date
$Duration = $EndTime - $StartTime

Write-Log "📊 === MODEL HEALTH PERSISTENCE TEST SUMMARY ===" "INFO"
Write-Log "⏱️ Total Duration: $($Duration.ToString('hh\:mm\:ss'))" "INFO"

# Check final status
$DataDir = "data"
$HealthFile = "$DataDir/model-health.json"

if (Test-Path $HealthFile) {
    $FileInfo = Get-Item $HealthFile
    $FileSize = $FileInfo.Length
    
    Write-Log "✅ Health file exists: $HealthFile" "SUCCESS"
    Write-Log "📊 File size: $FileSize bytes" "INFO"
    Write-Log "📅 Last modified: $($FileInfo.LastWriteTime)" "INFO"
    
    if ($FileSize -gt 0) {
        Write-Log "🎉 Model health persistence test PASSED!" "SUCCESS"
        Write-Log "💾 Health data is being persisted successfully" "SUCCESS"
        exit 0
    } else {
        Write-Log "⚠️ Health file is empty" "WARNING"
        Write-Log "🔍 Check if models are being marked as failed" "WARNING"
        exit 1
    }
} else {
    Write-Log "❌ Health file not found" "ERROR"
    Write-Log "🔍 Check if persistence is working correctly" "ERROR"
    exit 1
} 