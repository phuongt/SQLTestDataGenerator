# =====================================================================
# Oracle Connection Diagnostic Script
# =====================================================================
# PURPOSE: Chẩn đoán lỗi kết nối Oracle và kiểm tra các vấn đề
# USAGE: .\scripts\test-oracle-connection-diagnostic.ps1
# =====================================================================

param(
    [string]$ConnectionString = "Data Source=localhost:1521/XE;User Id=system;Password=22092012;Connection Timeout=120;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;"
)

# Set error handling
$ErrorActionPreference = "Stop"

# Colors for output
$Green = [System.ConsoleColor]::Green
$Red = [System.ConsoleColor]::Red
$Yellow = [System.ConsoleColor]::Yellow
$Cyan = [System.ConsoleColor]::Cyan
$White = [System.ConsoleColor]::White

function Write-ColoredOutput {
    param(
        [string]$Message, 
        [System.ConsoleColor]$Color = [System.ConsoleColor]::White
    )
    Write-Host $Message -ForegroundColor $Color
}

function Show-Header {
    Write-Host ""
    Write-ColoredOutput "===============================================" $Cyan
    Write-ColoredOutput "  Oracle Connection Diagnostic Tool" $Cyan  
    Write-ColoredOutput "===============================================" $Cyan
    Write-Host ""
}

function Test-OracleService {
    Write-ColoredOutput "🔍 Checking Oracle Service Status..." $Yellow
    
    try {
        $oracleService = Get-Service -Name "OracleServiceXE" -ErrorAction SilentlyContinue
        if ($oracleService) {
            if ($oracleService.Status -eq "Running") {
                Write-ColoredOutput "✅ Oracle Service XE is running" $Green
                return $true
            } else {
                Write-ColoredOutput "⚠️ Oracle Service XE exists but not running (Status: $($oracleService.Status))" $Yellow
                return $false
            }
        } else {
            Write-ColoredOutput "❌ Oracle Service XE not found" $Red
            return $false
        }
    }
    catch {
        Write-ColoredOutput "❌ Error checking Oracle service: $_" $Red
        return $false
    }
}

function Test-NetworkConnection {
    Write-ColoredOutput "🌐 Testing network connection to Oracle..." $Yellow
    
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $tcpClient.ConnectAsync("localhost", 1521).Wait(5000) | Out-Null
        
        if ($tcpClient.Connected) {
            Write-ColoredOutput "✅ Network connection to localhost:1521 successful" $Green
            $tcpClient.Close()
            return $true
        } else {
            Write-ColoredOutput "❌ Network connection to localhost:1521 failed" $Red
            return $false
        }
    }
    catch {
        Write-ColoredOutput "❌ Network connection error: $_" $Red
        return $false
    }
}

function Test-DirectOracleConnection {
    Write-ColoredOutput "🔌 Testing direct Oracle connection..." $Yellow
    
    try {
        # Load Oracle.ManagedDataAccess assembly
        $oracleAssembly = [System.Reflection.Assembly]::LoadWithPartialName("Oracle.ManagedDataAccess")
        if (-not $oracleAssembly) {
            Write-ColoredOutput "❌ Oracle.ManagedDataAccess assembly not found" $Red
            return $false
        }
        
        Write-ColoredOutput "✅ Oracle.ManagedDataAccess assembly loaded" $Green
        
        # Create connection
        $connection = New-Object Oracle.ManagedDataAccess.Client.OracleConnection($ConnectionString)
        $connection.Open()
        
        Write-ColoredOutput "✅ Direct Oracle connection successful" $Green
        
        # Test simple query
        $command = New-Object Oracle.ManagedDataAccess.Client.OracleCommand("SELECT SYSDATE FROM DUAL", $connection)
        $result = $command.ExecuteScalar()
        Write-ColoredOutput "✅ Query test successful: $result" $Green
        
        $connection.Close()
        return $true
    }
    catch {
        Write-ColoredOutput "❌ Direct Oracle connection failed: $_" $Red
        return $false
    }
}

function Test-ApplicationConnection {
    Write-ColoredOutput "🏗️ Testing application connection..." $Yellow
    
    try {
        # Build the project
        Write-ColoredOutput "Building project..." $White
        dotnet build SqlTestDataGenerator.sln --configuration Debug --verbosity minimal
        
        if ($LASTEXITCODE -ne 0) {
            Write-ColoredOutput "❌ Build failed" $Red
            return $false
        }
        
        Write-ColoredOutput "✅ Build successful" $Green
        
        # Run Oracle connection test
        Write-ColoredOutput "Running Oracle connection test..." $White
        dotnet test SqlTestDataGenerator.Tests --filter "OracleConnectionTest" --verbosity normal
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColoredOutput "✅ Application Oracle connection test passed" $Green
            return $true
        } else {
            Write-ColoredOutput "❌ Application Oracle connection test failed" $Red
            return $false
        }
    }
    catch {
        Write-ColoredOutput "❌ Application test error: $_" $Red
        return $false
    }
}

function Show-ConnectionStringAnalysis {
    Write-ColoredOutput "📋 Connection String Analysis:" $Cyan
    Write-ColoredOutput "Connection String: $ConnectionString" $White
    
    # Parse connection string
    $parts = $ConnectionString -split ';'
    foreach ($part in $parts) {
        if ($part.Trim()) {
            Write-ColoredOutput "  • $($part.Trim())" $White
        }
    }
    Write-Host ""
}

function Show-TroubleshootingSteps {
    Write-ColoredOutput "🔧 Troubleshooting Steps:" $Cyan
    
    Write-ColoredOutput "1. Check Oracle Installation:" $White
    Write-ColoredOutput "   - Verify Oracle XE is installed" $White
    Write-ColoredOutput "   - Check Oracle service is running: services.msc" $White
    Write-ColoredOutput "   - Verify Oracle client is installed" $White
    
    Write-ColoredOutput "2. Check Network/Firewall:" $White
    Write-ColoredOutput "   - Port 1521 should be open" $White
    Write-ColoredOutput "   - Check Oracle service is running: services.msc" $White
    Write-ColoredOutput "   - Firewall should allow Oracle connections" $White
    
    Write-ColoredOutput "3. Check Credentials:" $White
    Write-ColoredOutput "   - Verify username/password" $White
    Write-ColoredOutput "   - Test with SQL*Plus: sqlplus system/22092012@localhost:1521/XE" $White
    
    Write-ColoredOutput "4. Check Application Configuration:" $White
    Write-ColoredOutput "   - Verify Oracle.ManagedDataAccess NuGet package" $White
    Write-ColoredOutput "   - Check connection string format" $White
    Write-ColoredOutput "   - Verify database type is set to 'Oracle'" $White
    
    Write-Host ""
}

# =====================================================================
# MAIN EXECUTION
# =====================================================================

try {
    Show-Header
    Show-ConnectionStringAnalysis
    
    $results = @{}
    
    # Test 1: Oracle Service
    $results.Service = Test-OracleService
    
    # Test 2: Network Connection
    $results.Network = Test-NetworkConnection
    
    # Test 3: Direct Oracle Connection
    $results.Direct = Test-DirectOracleConnection
    
    # Test 4: Application Connection
    $results.Application = Test-ApplicationConnection
    
    # Summary
    Write-ColoredOutput "📊 Diagnostic Summary:" $Cyan
    Write-ColoredOutput "Oracle Service: $(if ($results.Service) { '✅ OK' } else { '❌ FAILED' })" $(if ($results.Service) { $Green } else { $Red })
    Write-ColoredOutput "Network Connection: $(if ($results.Network) { '✅ OK' } else { '❌ FAILED' })" $(if ($results.Network) { $Green } else { $Red })
    Write-ColoredOutput "Direct Connection: $(if ($results.Direct) { '✅ OK' } else { '❌ FAILED' })" $(if ($results.Direct) { $Green } else { $Red })
    Write-ColoredOutput "Application Test: $(if ($results.Application) { '✅ OK' } else { '❌ FAILED' })" $(if ($results.Application) { $Green } else { $Red })
    
    Write-Host ""
    
    if ($results.Service -and $results.Network -and $results.Direct -and $results.Application) {
        Write-ColoredOutput "🎉 All tests passed! Oracle connection should work properly." $Green
    } else {
        Write-ColoredOutput "⚠️ Some tests failed. Check the issues above." $Yellow
        Show-TroubleshootingSteps
    }
}
catch {
    Write-ColoredOutput "💥 Diagnostic script failed: $_" $Red
    Write-ColoredOutput "Stack trace: $($_.ScriptStackTrace)" $Red
    exit 1
}

Write-Host ""
Write-ColoredOutput "=== Diagnostic Complete ===" $Cyan 