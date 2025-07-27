# =====================================================================
# Connection Debug Script for SqlTestDataGenerator
# =====================================================================
# PURPOSE: Debug connection issues và test các connection string khác nhau
# USAGE: .\scripts\test-connection-debug.ps1
# =====================================================================

param(
    [string]$DatabaseType = "MySQL",
    [string]$ConnectionString = ""
)

# Colors for output
$Green = [System.ConsoleColor]::Green
$Red = [System.ConsoleColor]::Red
$Yellow = [System.ConsoleColor]::Yellow
$Cyan = [System.ConsoleColor]::Cyan
$White = [System.ConsoleColor]::White

function Write-ColoredOutput {
    param([string]$Message, [System.ConsoleColor]$Color = $White)
    Write-Host $Message -ForegroundColor $Color
}

function Test-MySQLConnection {
    param([string]$ConnectionString, [string]$TestName)
    
    Write-ColoredOutput "`n🔍 Testing: $TestName" $Cyan
    Write-ColoredOutput "Connection: $($ConnectionString.Replace('Pwd=22092012', 'Pwd=***').Replace('Password=22092012', 'Password=***'))" $White
    
    try {
        # Test using .NET application
        $testScript = @"
using System;
using MySqlConnector;

class ConnectionTest {
    static void Main() {
        try {
            var connection = new MySqlConnection(@"$ConnectionString");
            connection.Open();
            Console.WriteLine("SUCCESS");
            
            var command = connection.CreateCommand();
            command.CommandText = "SELECT VERSION(), DATABASE(), USER(), NOW()";
            var reader = command.ExecuteReader();
            
            if (reader.Read()) {
                Console.WriteLine($"VERSION:{reader.GetString(0)}");
                Console.WriteLine($"DATABASE:{reader.GetString(1)}");
                Console.WriteLine($"USER:{reader.GetString(2)}");
                Console.WriteLine($"TIME:{reader.GetDateTime(3)}");
            }
            
            reader.Close();
            connection.Close();
        }
        catch (Exception ex) {
            Console.WriteLine($"ERROR:{ex.Message}");
        }
    }
}
"@
        
        # Save test script
        $testScriptPath = "temp_connection_test.cs"
        $testScript | Out-File -FilePath $testScriptPath -Encoding UTF8
        
        # Compile and run
        $output = & dotnet run --project SqlTestDataGenerator.Core --connection-test "$ConnectionString" 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColoredOutput "✅ Connection successful!" $Green
            foreach ($line in $output) {
                if ($line.StartsWith("VERSION:")) {
                    Write-ColoredOutput "   MySQL Version: $($line.Substring(8))" $White
                }
                elseif ($line.StartsWith("DATABASE:")) {
                    Write-ColoredOutput "   Database: $($line.Substring(9))" $White
                }
                elseif ($line.StartsWith("USER:")) {
                    Write-ColoredOutput "   User: $($line.Substring(5))" $White
                }
                elseif ($line.StartsWith("TIME:")) {
                    Write-ColoredOutput "   Server Time: $($line.Substring(5))" $White
                }
            }
            return $true
        } else {
            Write-ColoredOutput "❌ Connection failed: $output" $Red
            return $false
        }
    }
    catch {
        Write-ColoredOutput "❌ Connection failed: $($_.Exception.Message)" $Red
        return $false
    }
    finally {
        # Cleanup
        if (Test-Path $testScriptPath) {
            Remove-Item $testScriptPath -Force
        }
    }
}

function Test-MySQLService {
    Write-ColoredOutput "`n🔧 Checking MySQL Service Status..." $Yellow
    
    try {
        $service = Get-Service -Name "*mysql*" -ErrorAction SilentlyContinue
        if ($service) {
            Write-ColoredOutput "Found MySQL service: $($service.Name)" $White
            Write-ColoredOutput "Status: $($service.Status)" $(if ($service.Status -eq "Running") { $Green } else { $Red })
            Write-ColoredOutput "Start Type: $($service.StartType)" $White
        } else {
            Write-ColoredOutput "❌ No MySQL service found!" $Red
        }
    }
    catch {
        Write-ColoredOutput "❌ Error checking MySQL service: $($_.Exception.Message)" $Red
    }
}

function Test-NetworkConnectivity {
    Write-ColoredOutput "`n🌐 Testing Network Connectivity..." $Yellow
    
    $testHosts = @("localhost", "127.0.0.1")
    $testPorts = @(3306, 3307, 3308)
    
    foreach ($testHost in $testHosts) {
        foreach ($testPort in $testPorts) {
            try {
                $tcp = New-Object System.Net.Sockets.TcpClient
                $tcp.ConnectAsync($testHost, $testPort).Wait(3000) | Out-Null
                
                if ($tcp.Connected) {
                    Write-ColoredOutput "✅ $testHost`:$testPort - Connected" $Green
                    $tcp.Close()
                } else {
                    Write-ColoredOutput "❌ $testHost`:$testPort - Failed" $Red
                }
            }
            catch {
                Write-ColoredOutput "❌ $testHost`:$testPort - $($_.Exception.Message)" $Red
            }
        }
    }
}

function Test-SimpleConnection {
    Write-ColoredOutput "`n🔌 Testing Simple Connection..." $Yellow
    
    # Test using existing test project
    try {
        $testResult = & dotnet test SqlTestDataGenerator.Tests/SqlTestDataGenerator.Tests.csproj --filter "TestCategory=ConnectionTest" --logger "console;verbosity=detailed" 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColoredOutput "✅ Connection tests passed!" $Green
        } else {
            Write-ColoredOutput "❌ Connection tests failed!" $Red
            Write-ColoredOutput $testResult $Red
        }
    }
    catch {
        Write-ColoredOutput "❌ Error running connection tests: $($_.Exception.Message)" $Red
    }
}

# =====================================================================
# MAIN EXECUTION
# =====================================================================

Write-ColoredOutput "===============================================" $Cyan
Write-ColoredOutput "  Connection Debug Script" $Cyan
Write-ColoredOutput "===============================================" $Cyan

# Test MySQL Service
Test-MySQLService

# Test Network Connectivity
Test-NetworkConnectivity

# Test Simple Connection using existing tests
Test-SimpleConnection

# Test various connection strings using .NET
$testConnections = @(
    @{
        Name = "Default MySQL (password: 22092012)"
        ConnectionString = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;"
    },
    @{
        Name = "MySQL without password"
        ConnectionString = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;"
    },
    @{
        Name = "MySQL with root password"
        ConnectionString = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=root;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;"
    },
    @{
        Name = "MySQL test database"
        ConnectionString = "Server=localhost;Port=3306;Database=test;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;"
    },
    @{
        Name = "MySQL mysql database"
        ConnectionString = "Server=localhost;Port=3306;Database=mysql;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;"
    }
)

Write-ColoredOutput "`n🔌 Testing MySQL Connections..." $Yellow

$workingConnections = @()
foreach ($test in $testConnections) {
    $success = Test-MySQLConnection -ConnectionString $test.ConnectionString -TestName $test.Name
    if ($success) {
        $workingConnections += $test
    }
}

# Summary
Write-ColoredOutput "`n📊 SUMMARY" $Cyan
Write-ColoredOutput "===============================================" $Cyan
Write-ColoredOutput "Total connections tested: $($testConnections.Count)" $White
Write-ColoredOutput "Working connections: $($workingConnections.Count)" $(if ($workingConnections.Count -gt 0) { $Green } else { $Red })

if ($workingConnections.Count -gt 0) {
    Write-ColoredOutput "`n✅ Working connection strings:" $Green
    foreach ($conn in $workingConnections) {
        Write-ColoredOutput "   - $($conn.Name)" $White
        Write-ColoredOutput "     $($conn.ConnectionString.Replace('Pwd=22092012', 'Pwd=***').Replace('Password=22092012', 'Password=***'))" $White
    }
} else {
    Write-ColoredOutput "`n❌ No working connections found!" $Red
    Write-ColoredOutput "`n🔧 Troubleshooting steps:" $Yellow
    Write-ColoredOutput "1. Start MySQL service" $White
    Write-ColoredOutput "2. Check if MySQL is running on port 3306" $White
    Write-ColoredOutput "3. Verify root password" $White
    Write-ColoredOutput "4. Create 'my_database' if it doesn't exist" $White
    Write-ColoredOutput "5. Check firewall settings" $White
}

Write-ColoredOutput "`n===============================================" $Cyan 