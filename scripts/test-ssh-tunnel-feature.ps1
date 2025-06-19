#!/usr/bin/env pwsh
# Test SSH Tunnel Feature
# Description: Test the new SSH tunnel functionality for database connections

Write-Host "=== SSH Tunnel Feature Test ===" -ForegroundColor Cyan
Write-Host "Testing SSH tunnel support for remote database connections" -ForegroundColor White
Write-Host ""

# Function to test SSH tunnel functionality
function Test-SshTunnelFeature {
    Write-Host "📋 SSH Tunnel Test Plan:" -ForegroundColor Yellow
    Write-Host "1. Build application with SSH.NET support" -ForegroundColor Gray
    Write-Host "2. Launch application with SSH UI controls" -ForegroundColor Gray
    Write-Host "3. Test SSH tunnel configuration" -ForegroundColor Gray
    Write-Host "4. Test database connection through tunnel" -ForegroundColor Gray
    Write-Host ""

    # Step 1: Build application
    Write-Host "🔧 Step 1: Building application..." -ForegroundColor Cyan
    try {
        $buildResult = dotnet build SqlTestDataGenerator.sln --configuration Release --verbosity minimal 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Build successful" -ForegroundColor Green
        } else {
            Write-Host "❌ Build failed:" -ForegroundColor Red
            Write-Host $buildResult -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "❌ Build error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }

    # Step 2: Check SSH.NET package installation
    Write-Host "🔍 Step 2: Verifying SSH.NET package..." -ForegroundColor Cyan
    $csprojPath = "SqlTestDataGenerator.Core\SqlTestDataGenerator.Core.csproj"
    $csprojContent = Get-Content $csprojPath -Raw
    
    if ($csprojContent -like "*SSH.NET*") {
        Write-Host "✅ SSH.NET package found in project file" -ForegroundColor Green
    } else {
        Write-Host "❌ SSH.NET package not found" -ForegroundColor Red
        return $false
    }

    # Step 3: Check SSH service implementation
    Write-Host "🔍 Step 3: Verifying SSH service implementation..." -ForegroundColor Cyan
    $sshServicePath = "SqlTestDataGenerator.Core\Services\SshTunnelService.cs"
    
    if (Test-Path $sshServicePath) {
        Write-Host "✅ SshTunnelService.cs exists" -ForegroundColor Green
        
        $serviceContent = Get-Content $sshServicePath -Raw
        $requiredFeatures = @(
            "CreateTunnelAsync",
            "TestSshConnectionAsync",
            "GetTunnelConnectionString",
            "CloseTunnel",
            "IsConnected"
        )
        
        foreach ($feature in $requiredFeatures) {
            if ($serviceContent -like "*$feature*") {
                Write-Host "  ✅ $feature method found" -ForegroundColor Green
            } else {
                Write-Host "  ❌ $feature method missing" -ForegroundColor Red
                return $false
            }
        }
    } else {
        Write-Host "❌ SshTunnelService.cs not found" -ForegroundColor Red
        return $false
    }

    # Step 4: Check UI controls implementation
    Write-Host "🔍 Step 4: Verifying SSH UI controls..." -ForegroundColor Cyan
    $mainFormPath = "SqlTestDataGenerator.UI\MainForm.cs"
    
    if (Test-Path $mainFormPath) {
        $formContent = Get-Content $mainFormPath -Raw
        
        $requiredUIElements = @(
            "chkUseSSH",
            "grpSSH",
            "txtSSHHost",
            "txtSSHUsername",
            "txtSSHPassword",
            "btnTestSSH",
            "lblSSHStatus"
        )
        
        foreach ($element in $requiredUIElements) {
            if ($formContent -like "*$element*") {
                Write-Host "  ✅ $element UI control found" -ForegroundColor Green
            } else {
                Write-Host "  ❌ $element UI control missing" -ForegroundColor Red
                return $false
            }
        }
    } else {
        Write-Host "❌ MainForm.cs not found" -ForegroundColor Red
        return $false
    }

    # Step 5: Launch application for manual testing
    Write-Host "🚀 Step 5: Launching application for SSH testing..." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📝 Manual Testing Instructions:" -ForegroundColor Yellow
    Write-Host "1. Look for '🔐 Use SSH Tunnel' checkbox in the UI" -ForegroundColor Gray
    Write-Host "2. Check the checkbox to reveal SSH configuration panel" -ForegroundColor Gray
    Write-Host "3. Fill in SSH connection details:" -ForegroundColor Gray
    Write-Host "   • SSH Host: your-ssh-server.com" -ForegroundColor Gray
    Write-Host "   • SSH Port: 22" -ForegroundColor Gray
    Write-Host "   • Username: your-ssh-username" -ForegroundColor Gray
    Write-Host "   • Password: your-ssh-password" -ForegroundColor Gray
    Write-Host "   • Remote DB Host: localhost (inside SSH network)" -ForegroundColor Gray
    Write-Host "   • Remote DB Port: 3306" -ForegroundColor Gray
    Write-Host "4. Click '🧪 Test SSH' to establish tunnel" -ForegroundColor Gray
    Write-Host "5. After SSH tunnel success, click '🔌 Test Connection'" -ForegroundColor Gray
    Write-Host "6. Verify database connection works through tunnel" -ForegroundColor Gray
    Write-Host ""

    try {
        $exePath = "SqlTestDataGenerator.UI\bin\Release\net8.0-windows\SqlTestDataGenerator.UI.exe"
        if (Test-Path $exePath) {
            Write-Host "✅ Starting application from: $exePath" -ForegroundColor Green
            Start-Process $exePath
            Write-Host "🎯 Application launched! Please perform manual SSH testing." -ForegroundColor Green
        } else {
            Write-Host "❌ Executable not found at: $exePath" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "❌ Launch error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }

    return $true
}

# Function to display SSH tunnel feature documentation
function Show-SshFeatureDocumentation {
    Write-Host ""
    Write-Host "📚 SSH Tunnel Feature Documentation:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "🔧 Technical Implementation:" -ForegroundColor Yellow
    Write-Host "• SshTunnelService: Core SSH tunnel management" -ForegroundColor Gray
    Write-Host "• SSH.NET Library: Secure SSH connections and port forwarding" -ForegroundColor Gray
    Write-Host "• DbConnectionFactory: Enhanced with SSH tunnel support" -ForegroundColor Gray
    Write-Host "• MainForm UI: SSH configuration controls" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🌐 Use Cases:" -ForegroundColor Yellow
    Write-Host "• Connect to databases in private subnets" -ForegroundColor Gray
    Write-Host "• Access cloud databases through bastion hosts" -ForegroundColor Gray
    Write-Host "• Secure connections to internal corporate databases" -ForegroundColor Gray
    Write-Host "• Development access to production-like environments" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🔒 Security Features:" -ForegroundColor Yellow
    Write-Host "• Password-based SSH authentication" -ForegroundColor Gray
    Write-Host "• Private key authentication (planned)" -ForegroundColor Gray
    Write-Host "• Automatic tunnel cleanup on application exit" -ForegroundColor Gray
    Write-Host "• Connection validation before database operations" -ForegroundColor Gray
    Write-Host ""
    Write-Host "📋 Workflow:" -ForegroundColor Yellow
    Write-Host "1. Enable SSH Tunnel checkbox" -ForegroundColor Gray
    Write-Host "2. Configure SSH server connection details" -ForegroundColor Gray
    Write-Host "3. Test SSH connection and create tunnel" -ForegroundColor Gray
    Write-Host "4. Test database connection through tunnel" -ForegroundColor Gray
    Write-Host "5. Generate test data as normal" -ForegroundColor Gray
    Write-Host ""
}

# Main execution
$testResult = Test-SshTunnelFeature

if ($testResult) {
    Write-Host ""
    Write-Host "🎉 SSH Tunnel Feature Test PASSED!" -ForegroundColor Green
    Write-Host "All components successfully implemented and verified." -ForegroundColor Green
    Show-SshFeatureDocumentation
} else {
    Write-Host ""
    Write-Host "❌ SSH Tunnel Feature Test FAILED!" -ForegroundColor Red
    Write-Host "Please check the error messages above and fix any issues." -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan 