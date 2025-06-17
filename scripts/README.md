# 📜 Scripts Documentation

Scripts folder chứa các PowerShell scripts để build, run và test SqlTestDataGenerator project.

## 📋 Available Scripts

### 1. 🏗️ build-and-run-ui.ps1
**Purpose**: Build và run SqlTestDataGenerator.UI Windows Forms application

**Usage**:
```powershell
# Basic run (Debug mode)
.\scripts\build-and-run-ui.ps1

# Clean build
.\scripts\build-and-run-ui.ps1 -Clean

# Release build
.\scripts\build-and-run-ui.ps1 -Release

# Clean Release build
.\scripts\build-and-run-ui.ps1 -Clean -Release
```

**Features**:
- ✅ Automatic .NET SDK detection
- ✅ Project file validation
- ✅ Clean build option
- ✅ Debug/Release configuration
- ✅ Colored output with status indicators
- ✅ Error handling with detailed messages

**Requirements**:
- .NET 6+ SDK
- Windows OS (Windows Forms support)
- SqlTestDataGenerator.UI project

---

### 2. 🧪 run-tc001-test.ps1
**Purpose**: Run specific test method `TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI`

**Usage**:
```powershell
# Basic test run
.\scripts\run-tc001-test.ps1

# Verbose output
.\scripts\run-tc001-test.ps1 -Verbose

# Debug mode
.\scripts\run-tc001-test.ps1 -Debug

# Release configuration
.\scripts\run-tc001-test.ps1 -Configuration Release
```

**Test Information**:
- **Test Class**: ExecuteQueryWithTestDataAsyncDemoTests
- **Test Method**: TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI  
- **Categories**: AI-MySQL-Real, Integration
- **Purpose**: Test AI-enhanced data generation với complex SQL query

**Expected Behavior**:
- ✅ WITH Gemini API + MySQL: Generate 15 meaningful records
- ⚠️ WITHOUT API key: Fallback to constraint-based generation
- ❌ WITHOUT MySQL: Fail with connection error (expected)
- 🎯 Validates: AI-enhanced data generation for complex SQL scenarios

**Features**:
- ✅ Automatic test project build
- ✅ Detailed test execution reporting
- ✅ Performance timing
- ✅ Common failure guidance
- ✅ Log location information

**Requirements**:
- .NET 6+ SDK
- MSTest framework
- SqlTestDataGenerator.Tests project
- Optional: MySQL connection, Gemini API Key

---

### 3. 🔧 integration_test.ps1
**Purpose**: Integration testing script (moved from root)

### 3. 🚀 quick-test.ps1
**Purpose**: Fast execution of TC001 test method with minimal output

**Usage**:
```powershell
# Quick test run
.\scripts\quick-test.ps1

# Minimal output
.\scripts\quick-test.ps1 -Quiet
```

**Features**:
- ✅ Fast test execution without verbose logging
- ✅ Automatic build detection
- ✅ Performance timing
- ✅ Minimal output for CI/CD integration

---

### 4. 🔧 integration_test.ps1
**Purpose**: Integration testing script (moved from root)

### 5. ✅ verify_tc001_fix.ps1  
**Purpose**: Verification script for TC001 fixes (moved from root)

## 🎯 Quick Start Guide

### Running the UI Application:
```powershell
# Navigate to project root
cd C:\Customize\04.GenData

# Run UI application
.\scripts\build-and-run-ui.ps1
```

### Running TC001 Test:
```powershell
# Navigate to project root  
cd C:\Customize\04.GenData

# Run specific test with verbose output
.\scripts\run-tc001-test.ps1 -Verbose
```

## 📊 Output Examples

### build-and-run-ui.ps1 Output:
```
===============================================
  SqlTestDataGenerator.UI Build & Run Script
===============================================

📋 Project Information:
  • Project: SqlTestDataGenerator.UI
  • Framework: net6.0-windows
  • Configuration: Debug
  • Clean Build: No

🔍 Checking prerequisites...
✅ .NET SDK found: 6.0.XXX
✅ Project file found: SqlTestDataGenerator.UI\SqlTestDataGenerator.UI.csproj
✅ App settings found: SqlTestDataGenerator.UI\appsettings.json

🔨 Building SqlTestDataGenerator.UI (Debug)...
✅ Build completed successfully!

🚀 Starting SqlTestDataGenerator.UI...
📍 Press Ctrl+C to stop the application
```

### run-tc001-test.ps1 Output:
```
===============================================
  TC001 Complex Vowis SQL Test Runner
===============================================

📋 Test Information:
  • Test Class: ExecuteQueryWithTestDataAsyncDemoTests
  • Test Method: TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI
  • Categories: AI-MySQL-Real, Integration

🔍 Checking prerequisites...
✅ .NET SDK found: 6.0.XXX
✅ Test project found: SqlTestDataGenerator.Tests\SqlTestDataGenerator.Tests.csproj
✅ Test class found: SqlTestDataGenerator.Tests\ExecuteQueryWithTestDataAsyncDemoTests.cs
✅ Gemini API Key configuration detected

📁 Log Locations:
  • Test Results: SqlTestDataGenerator.Tests\TestResults\
  • Application Logs: logs\
  • Test Logs: SqlTestDataGenerator.Tests\logs\

🔨 Building test project...
✅ Test project build completed successfully!

🧪 Running TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI...

🎯 Target Test Method: SqlTestDataGenerator.Tests.ExecuteQueryWithTestDataAsyncDemoTests.TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI
📋 Test Categories: AI-MySQL-Real, Integration
🔧 Configuration: Debug

📝 Expected Test Behavior:
  • WITH Gemini API + MySQL: Generate 15 meaningful records
  • WITHOUT API key: Fallback to constraint-based generation
  • WITHOUT MySQL: Fail with connection error
  • Validates: AI-enhanced data generation for complex SQL
```

## 🚨 Common Issues & Solutions

### build-and-run-ui.ps1 Issues:

❌ **".NET SDK not found"**
- **Solution**: Install .NET 6+ SDK from https://dotnet.microsoft.com/download

❌ **"Project file not found"** 
- **Solution**: Run script from project root directory (where .sln file exists)

❌ **"Build failed"**
- **Solution**: Check dependencies, ensure all NuGet packages are restored

### run-tc001-test.ps1 Issues:

❌ **"Test failed with MySQL connection error"**
- **Solution**: Expected behavior without real MySQL connection

⚠️ **"Gemini API quota exceeded"**
- **Solution**: Expected behavior, indicates engine worked successfully  

⚠️ **"API key not found"**
- **Solution**: Test will fallback to constraint-based generation

## 📁 File Organization

After running scripts, files are organized as follows:

```
04.GenData/
├── scripts/           # All PowerShell scripts
│   ├── build-and-run-ui.ps1
│   ├── run-tc001-test.ps1  
│   ├── integration_test.ps1
│   ├── verify_tc001_fix.ps1
│   └── README.md
├── sql/              # SQL files and schemas
├── docs/             # Documentation and summaries  
├── reports/          # Test results and analysis
└── logs/             # Application logs
```

## 🔐 Security Notes

- Scripts validate prerequisites before execution
- No hardcoded credentials or sensitive data
- API keys read from configuration files only
- Error handling prevents sensitive information exposure

## 🔄 Maintenance

Scripts are self-documenting and include:
- Parameter validation
- Prerequisite checking  
- Detailed error messages
- Usage examples
- Colored output for clarity

For issues or improvements, check the script headers for detailed documentation. 