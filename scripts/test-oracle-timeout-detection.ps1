# Test Oracle Timeout Detection
# Phát hiện chỗ nào bị timeout trong Oracle test với AI

param(
    [switch]$Verbose = $false,
    [switch]$CleanStart = $false,
    [int]$TimeoutMinutes = 5
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   ORACLE TIMEOUT DETECTION TEST" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Initialize variables
$StartTime = Get-Date
$LogFile = "logs/oracle-timeout-detection-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').log"
$TimeoutMs = $TimeoutMinutes * 60 * 1000

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
}

# Navigate to test directory
$OriginalLocation = Get-Location
Push-Location "SqlTestDataGenerator.Tests"

try {
    Write-Log "🚀 Starting Oracle Timeout Detection Test" "INFO"
    Write-Log "📁 Test Directory: $(Get-Location)" "INFO"
    Write-Log "📄 Log File: $LogFile" "INFO"
    Write-Log "⏱️ Timeout: $TimeoutMinutes minutes ($TimeoutMs ms)" "INFO"
    
    # Build the project first
    Write-Log "🔨 Building test project..." "INFO"
    $BuildResult = dotnet build --verbosity normal 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Log "❌ Build failed with exit code: $LASTEXITCODE" "ERROR"
        Write-Log "Build failure details: $BuildResult" "ERROR"
        exit 1
    }
    Write-Log "✅ Build successful" "SUCCESS"
    
    # Test 1: Run Oracle test with detailed timing
    Write-Log "🧪 Test 1: Running Oracle test with AI and detailed timing" "INFO"
    
    $TestCommand = "dotnet test --filter `"OracleComplexQueryPhuong1989Tests`" --verbosity detailed --logger console;verbosity=detailed 2>&1"
    Write-Log "⚡ Running: $TestCommand" "DEBUG"
    
    $ProcessStartInfo = New-Object System.Diagnostics.ProcessStartInfo
    $ProcessStartInfo.FileName = "dotnet"
    $ProcessStartInfo.Arguments = "test --filter `"OracleComplexQueryPhuong1989Tests`" --verbosity detailed --logger console;verbosity=detailed"
    $ProcessStartInfo.RedirectStandardOutput = $true
    $ProcessStartInfo.RedirectStandardError = $true
    $ProcessStartInfo.UseShellExecute = $false
    $ProcessStartInfo.CreateNoWindow = $true
    
    $Process = New-Object System.Diagnostics.Process
    $Process.StartInfo = $ProcessStartInfo
    
    # Capture output streams
    $OutputBuilder = New-Object System.Text.StringBuilder
    $ErrorBuilder = New-Object System.Text.StringBuilder
    
    $OutputEvent = Register-ObjectEvent -InputObject $Process -EventName OutputDataReceived -Action {
        if ($Event.SourceEventArgs.Data) {
            $Event.MessageData.AppendLine($Event.SourceEventArgs.Data)
            # Real-time output for monitoring
            Write-Host $Event.SourceEventArgs.Data -ForegroundColor Gray
        }
    } -MessageData $OutputBuilder
    
    $ErrorEvent = Register-ObjectEvent -InputObject $Process -EventName ErrorDataReceived -Action {
        if ($Event.SourceEventArgs.Data) {
            $Event.MessageData.AppendLine($Event.SourceEventArgs.Data)
            # Real-time error output
            Write-Host $Event.SourceEventArgs.Data -ForegroundColor Red
        }
    } -MessageData $ErrorBuilder
    
    Write-Log "🔄 Starting Oracle test process..." "INFO"
    $Process.Start()
    $Process.BeginOutputReadLine()
    $Process.BeginErrorReadLine()
    
    # Wait for process with timeout
    $TestStartTime = Get-Date
    Write-Log "⏱️ Test started at: $TestStartTime" "INFO"
    
    if (!$Process.WaitForExit($TimeoutMs)) {
        $TestEndTime = Get-Date
        $TestDuration = $TestEndTime - $TestStartTime
        
        Write-Log "⚠️ Test timed out after $TimeoutMinutes minutes" "WARNING"
        Write-Log "⏱️ Test duration: $($TestDuration.ToString('hh\:mm\:ss'))" "WARNING"
        
        # Kill the process
        $Process.Kill()
        Write-Log "🔪 Process killed due to timeout" "WARNING"
        
        # Analyze partial output for timing information
        $TestOutput = $OutputBuilder.ToString()
        $TestError = $ErrorBuilder.ToString()
        
        Write-Log "📊 Analyzing partial output for timing information..." "INFO"
        
        # Look for timing patterns in output
        $TimingLines = $TestOutput -split "`n" | Where-Object { $_ -match "Step.*seconds|BOTTLENECK|Execution.*seconds" }
        if ($TimingLines.Count -gt 0) {
            Write-Log "⏰ Found timing information in output:" "INFO"
            foreach ($Line in $TimingLines) {
                Write-Log "   $($Line.Trim())" "INFO"
            }
        }
        
        # Look for EngineService timing
        $EngineTimingLines = $TestOutput -split "`n" | Where-Object { $_ -match "\[EngineService\].*Step.*seconds" }
        if ($EngineTimingLines.Count -gt 0) {
            Write-Log "🔧 EngineService timing breakdown:" "INFO"
            foreach ($Line in $EngineTimingLines) {
                Write-Log "   $($Line.Trim())" "INFO"
            }
        }
        
        # Look for bottleneck information
        $BottleneckLines = $TestOutput -split "`n" | Where-Object { $_ -match "BOTTLENECK" }
        if ($BottleneckLines.Count -gt 0) {
            Write-Log "🚨 BOTTLENECK detected:" "ERROR"
            foreach ($Line in $BottleneckLines) {
                Write-Log "   $($Line.Trim())" "ERROR"
            }
        }
        
        # Check for AI-related timing
        $AITimingLines = $TestOutput -split "`n" | Where-Object { $_ -match "AI.*generation|Gemini.*API|rate.*limit" }
        if ($AITimingLines.Count -gt 0) {
            Write-Log "🤖 AI-related timing:" "INFO"
            foreach ($Line in $AITimingLines) {
                Write-Log "   $($Line.Trim())" "INFO"
            }
        }
        
        Write-Log "❌ Test failed due to timeout" "ERROR"
        exit 1
    }
    
    # Clean up events
    Unregister-Event -SourceIdentifier $OutputEvent.Name
    Unregister-Event -SourceIdentifier $ErrorEvent.Name
    
    $TestEndTime = Get-Date
    $TestDuration = $TestEndTime - $TestStartTime
    $ExitCode = $Process.ExitCode
    
    $TestOutput = $OutputBuilder.ToString()
    $TestError = $ErrorBuilder.ToString()
    
    Write-Log "⏱️ Test completed at: $TestEndTime" "INFO"
    Write-Log "⏱️ Test duration: $($TestDuration.ToString('hh\:mm\:ss'))" "INFO"
    Write-Log "📊 Exit code: $ExitCode" "INFO"
    
    # Analyze test results
    if ($ExitCode -eq 0) {
        Write-Log "✅ Oracle test completed successfully" "SUCCESS"
        
        # Extract timing information
        $TimingLines = $TestOutput -split "`n" | Where-Object { $_ -match "Step.*seconds|BOTTLENECK|Execution.*seconds" }
        if ($TimingLines.Count -gt 0) {
            Write-Log "⏰ Timing breakdown:" "INFO"
            foreach ($Line in $TimingLines) {
                Write-Log "   $($Line.Trim())" "INFO"
            }
        }
        
        # Extract bottleneck information
        $BottleneckLines = $TestOutput -split "`n" | Where-Object { $_ -match "BOTTLENECK" }
        if ($BottleneckLines.Count -gt 0) {
            Write-Log "🚨 BOTTLENECK identified:" "WARNING"
            foreach ($Line in $BottleneckLines) {
                Write-Log "   $($Line.Trim())" "WARNING"
            }
        }
        
        # Check for AI performance
        $AITimingLines = $TestOutput -split "`n" | Where-Object { $_ -match "AI.*generation.*seconds" }
        if ($AITimingLines.Count -gt 0) {
            Write-Log "🤖 AI generation performance:" "INFO"
            foreach ($Line in $AITimingLines) {
                Write-Log "   $($Line.Trim())" "INFO"
            }
        }
        
    } else {
        Write-Log "❌ Oracle test failed with exit code: $ExitCode" "ERROR"
        
        # Analyze error output
        if ($TestError) {
            Write-Log "🔍 Error output:" "ERROR"
            $ErrorLines = $TestError -split "`n" | Where-Object { $_.Trim() -ne "" }
            foreach ($Line in $ErrorLines) {
                Write-Log "   $($Line.Trim())" "ERROR"
            }
        }
        
        # Look for timing information even in failed test
        $TimingLines = $TestOutput -split "`n" | Where-Object { $_ -match "Step.*seconds|BOTTLENECK" }
        if ($TimingLines.Count -gt 0) {
            Write-Log "⏰ Partial timing information:" "INFO"
            foreach ($Line in $TimingLines) {
                Write-Log "   $($Line.Trim())" "INFO"
            }
        }
    }
    
    # Test 2: Check model health file
    Write-Log "🧪 Test 2: Checking model health persistence" "INFO"
    
    $DataDir = "data"
    $HealthFile = "$DataDir/model-health.json"
    
    if (Test-Path $HealthFile) {
        $FileInfo = Get-Item $HealthFile
        Write-Log "✅ Model health file exists: $HealthFile" "SUCCESS"
        Write-Log "📊 File size: $($FileInfo.Length) bytes" "INFO"
        Write-Log "📅 Last modified: $($FileInfo.LastWriteTime)" "INFO"
        
        # Read health file content
        try {
            $HealthContent = Get-Content $HealthFile -Raw
            $HealthData = $HealthContent | ConvertFrom-Json
            
            $ModelCount = ($HealthData.PSObject.Properties | Measure-Object).Count
            Write-Log "📊 Health data contains $ModelCount models" "INFO"
            
            # Check for models with recovery times
            $ModelsWithRecovery = @()
            foreach ($Model in $HealthData.PSObject.Properties.Name) {
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
            Write-Log "⚠️ Failed to parse health file: $($_.Exception.Message)" "WARNING"
        }
    } else {
        Write-Log "❌ Model health file not found: $HealthFile" "ERROR"
    }
    
}
finally {
    Pop-Location
    Write-Log "Returned to original location: $(Get-Location)" "DEBUG"
}

# Generate Summary Report
$EndTime = Get-Date
$Duration = $EndTime - $StartTime

Write-Log "📊 === ORACLE TIMEOUT DETECTION SUMMARY ===" "INFO"
Write-Log "⏱️ Total Duration: $($Duration.ToString('hh\:mm\:ss'))" "INFO"
Write-Log "📄 Log File: $LogFile" "INFO"

# Check final status
if ($ExitCode -eq 0) {
    Write-Log "🎉 Oracle timeout detection test PASSED!" "SUCCESS"
    Write-Log "✅ Test completed within timeout limit" "SUCCESS"
    Write-Log "📋 Check log file for detailed timing breakdown" "INFO"
    exit 0
} else {
    Write-Log "❌ Oracle timeout detection test FAILED!" "ERROR"
    Write-Log "🔍 Check log file for detailed error analysis" "ERROR"
    exit 1
} 