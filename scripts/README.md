# Scripts Directory

This directory contains PowerShell scripts for automating various tasks in the SqlTestDataGenerator project.

## 🎯 Main Scripts (Essential Operations)

### 1. **final-test.ps1** - Comprehensive Testing Suite
**Purpose:** Run complete complex test cases for both MySQL and Oracle databases

**Usage:**
```powershell
# Run all tests (MySQL + Oracle + Cross-database)
.\final-test.ps1

# Run only MySQL tests
.\final-test.ps1 -DatabaseType MySQL

# Run only Oracle tests  
.\final-test.ps1 -DatabaseType Oracle

# Run with verbose output and generate detailed report
.\final-test.ps1 -Verbose -GenerateReport
```

**Features:**
- ✅ MySQL complex SQL generation tests
- ✅ Oracle complex query tests (including Phuong1989 test case)
- ✅ Cross-database integration tests
- ✅ AI integration tests
- ✅ Automatic report generation
- ✅ Comprehensive logging with timestamps
- ✅ Pass/fail summary with recommendations

### 2. **build-and-run-ui.ps1** - UI Development & Testing
**Purpose:** Build and launch the Windows Forms UI application

**Usage:**
```powershell
# Build and run UI with default settings
.\build-and-run-ui.ps1

# Build and run with specific database connection
.\build-and-run-ui.ps1 -TestMode
```

**Features:**
- ✅ Automatic project building
- ✅ UI application launch
- ✅ Error handling and diagnostics
- ✅ Development mode support

### 3. **create-single-file-deploy.ps1** - Production Deployment
**Purpose:** Create single-file deployment package for production

**Usage:**
```powershell
# Create deployment package
.\create-single-file-deploy.ps1

# Clean previous builds and create fresh deployment
.\create-single-file-deploy.ps1 -Clean
```

**Features:**
- ✅ Single-file executable generation
- ✅ All dependencies included
- ✅ Production-ready optimization
- ✅ Deployment package creation

---

## 📁 Archive Folder

All legacy and specialized testing scripts have been moved to the `archive/` subdirectory for reference and backward compatibility.

### Archived Scripts Include:
- **Testing Scripts:** Various test automation scripts for specific features
- **Integration Scripts:** Database connection and integration testing
- **Debug Scripts:** Troubleshooting and diagnostic utilities  
- **Workflow Scripts:** Specialized workflow testing scenarios
- **Legacy Deploy Scripts:** Previous deployment automation

### Accessing Archived Scripts:
```powershell
# Navigate to archive
cd archive

# List all archived scripts
ls *.ps1

# Run an archived script
.\archive\test-oracle-connection.ps1
```

---

## 🚀 Quick Start Guide

### For Development:
1. **Test the system:** `.\final-test.ps1`
2. **Run the UI:** `.\build-and-run-ui.ps1`
3. **Deploy for production:** `.\create-single-file-deploy.ps1`

### For Testing Only:
```powershell
# Test specific database
.\final-test.ps1 -DatabaseType MySQL
.\final-test.ps1 -DatabaseType Oracle

# Generate detailed test report
.\final-test.ps1 -Verbose -GenerateReport
```

### For Production Deployment:
```powershell
# Create single-file deployment
.\create-single-file-deploy.ps1
```

---

## 📋 Script Dependencies

**Prerequisites:**
- .NET 6 SDK or later
- PowerShell 5.1 or PowerShell Core
- Visual Studio or Build Tools
- Database connections (MySQL/Oracle) for integration tests

**Required Environment:**
- Windows 10/11 or Windows Server
- Sufficient disk space for builds and deployments
- Network access for AI API calls (if enabled)

---

## 🛠️ Troubleshooting

### Common Issues:

**Build Failures:**
```powershell
# Clean and rebuild
dotnet clean
dotnet restore
.\build-and-run-ui.ps1
```

**Test Failures:**
```powershell
# Check logs
cat logs\final-test-*.log

# Run database-specific tests
.\final-test.ps1 -DatabaseType MySQL -Verbose
```

**Deployment Issues:**
```powershell
# Clean previous builds
.\create-single-file-deploy.ps1 -Clean
```

### Getting Help:
- Check script output for detailed error messages
- Review generated log files in `logs/` directory
- Consult archived scripts for specific use cases
- Review test reports in `reports/` directory

---

## 📝 Notes

- **Script Organization:** The 3 main scripts cover all essential operations
- **Archive Access:** Legacy scripts remain available in `archive/` folder
- **Logging:** All main scripts generate detailed logs
- **Reports:** Test results are automatically documented
- **Modularity:** Each script is self-contained and can run independently

For detailed technical documentation, see the project's main documentation files. 