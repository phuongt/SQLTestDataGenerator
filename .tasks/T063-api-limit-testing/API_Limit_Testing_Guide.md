# 🚀 API Limit Testing Guide

**Task**: T063 - Create API AI Limit Testing Suite  
**Date**: January 6, 2025  
**Purpose**: Comprehensive testing để kiểm tra API AI có bị limit hay chưa

---

## 📋 OVERVIEW

Hệ thống SqlTestDataGenerator sử dụng Gemini AI API để generate intelligent data. Để đảm bảo ứng dụng hoạt động ổn định, chúng ta cần kiểm tra và handle các API limit scenarios:

- **Daily API Limits** - Quota hàng ngày
- **Rate Limiting** - Giới hạn số calls per second/minute  
- **Quota Exceeded** - Khi vượt quá giới hạn
- **Fallback Behavior** - Chuyển sang Bogus generation khi AI không available

---

## 🧪 TEST SUITES CREATED

### 1. **APILimitTestSuite.cs** - Comprehensive API Limit Testing
```csharp
[TestClass]
[TestCategory("API-Limit-Testing")]
public class APILimitTestSuite
```

**Coverage**:
- ✅ Daily API usage statistics tracking
- ✅ Rate limiting behavior validation  
- ✅ Quota exceeded simulation
- ✅ Model health and rotation testing
- ⚠️ Real API limit detection (optional)

**Key Tests**:
- `Test_DailyAPIUsageStatistics_ShouldReturnCurrentStatus()`
- `Test_APIAvailabilityChecking_ShouldReflectCurrentStatus()`
- `Test_QuotaExceededSimulation_ShouldHandleGracefully()`
- `Test_ModelHealthStatus_ShouldReflectAPIAvailability()`

### 2. **MockAPILimitService.cs** - Mock Service Testing
```csharp
public class MockAPILimitService
public enum APILimitScenario
```

**Scenarios Supported**:
- 🟢 **Normal** - API available
- 🔴 **DailyLimitReached** - Cannot call until tomorrow
- 🟡 **RateLimited** - Must wait between calls
- 🚫 **QuotaExceeded** - Similar to daily limit
- 🌐 **NetworkError** - Cannot reach API

**Mock Tests**:
- `Test_NormalScenario_ShouldAllowAPICalls()`
- `Test_DailyLimitReached_ShouldBlockCalls()`
- `Test_RateLimiting_ShouldEnforceDelay()`
- `Test_QuotaExceeded_ShouldHandleGracefully()`

### 3. **APILimitFallbackTests.cs** - Fallback Behavior Testing  
```csharp
[TestClass]
[TestCategory("API-Fallback")]
public class APILimitFallbackTests
```

**Fallback Scenarios**:
- 🔑 **No API Key** - Should fallback to Bogus
- ❌ **Invalid API Key** - Should fallback gracefully
- 🔽 **UseAI=false** - Should skip AI completely
- ⚡ **Performance** - Fallback should be faster than AI

**Key Tests**:
- `Test_NoAPIKey_ShouldFallbackToBogusGeneration()`
- `Test_InvalidAPIKey_ShouldFallbackToBogusGeneration()`
- `Test_UseAIFalse_ShouldSkipAICompletely()`
- `Test_FallbackPerformance_ShouldBeFasterThanAI()`

---

## 🛠️ AUTOMATED TESTING SCRIPT

### **test-api-limits.ps1** - PowerShell Testing Script

**Usage Examples**:
```powershell
# Run all API limit tests
.\scripts\test-api-limits.ps1

# Run specific category
.\scripts\test-api-limits.ps1 -TestCategory "Daily-Limits"

# Quick essential tests only
.\scripts\test-api-limits.ps1 -QuickTest -Verbose

# Test specific scenarios
.\scripts\test-api-limits.ps1 -TestCategory "Mock-API-Limits"
```

**Features**:
- ✅ Prerequisite checking (dotnet, project, API key)
- ✅ Automated build and test execution
- ✅ Color-coded results và detailed reporting
- ✅ Category-based test filtering
- ✅ Quick test mode for CI/CD
- ✅ Real API testing (với confirmation)

---

## 🎯 TESTING STRATEGY

### 1. **Daily Development Testing**
```bash
# Quick test để verify API limits không block development
.\scripts\test-api-limits.ps1 -QuickTest
```

### 2. **Pre-Release Testing**  
```bash
# Full test suite trước khi release
.\scripts\test-api-limits.ps1 -TestCategory "All" -Verbose
```

### 3. **Production Monitoring**
```bash
# Check current API quota status
.\scripts\test-api-limits.ps1 -TestCategory "Daily-Limits"
```

### 4. **Troubleshooting API Issues**
```bash
# Test fallback behavior
.\scripts\test-api-limits.ps1 -TestCategory "Fallback-Behavior"

# Test mock scenarios
.\scripts\test-api-limits.ps1 -TestCategory "Mock-API-Limits"
```

---

## 📊 API USAGE MONITORING

### **Real-time Statistics Available**:
```csharp
var stats = flashService.GetAPIUsageStatistics();
// Returns:
// - DailyCallsUsed: Current usage count
// - DailyCallLimit: Maximum allowed per day  
// - DailyCallsRemaining: Calls left today
// - DailyResetTime: When quota resets
// - CanCallNow: Boolean availability
// - NextCallableTime: When next call allowed
```

### **Health Monitoring**:
```csharp
var modelStats = flashService.GetModelStatistics();
// Returns:
// - TotalModels: Available Flash models
// - HealthyModels: Currently working models
// - FailedModels: Models that hit limits
// - ModelsByTier: Distribution by model tier
```

---

## 🚨 ERROR SCENARIOS & HANDLING

### 1. **Daily Limit Reached**
```
❌ Exception: "Daily API limit reached. Next callable time: 2025-01-07 00:00:00 UTC"
✅ Expected: Application falls back to Bogus generation
✅ User Experience: Data generation continues seamlessly
```

### 2. **Rate Limited**
```
❌ Exception: "Rate limited. Next callable time: 2025-01-06 14:30:15 UTC"  
✅ Expected: Application waits or falls back
✅ User Experience: Minor delay or fallback
```

### 3. **Quota Exceeded**
```
❌ Exception: "Quota exceeded. Please try again tomorrow."
✅ Expected: Immediate fallback to Bogus
✅ User Experience: No interruption
```

### 4. **Network Error**
```
❌ Exception: "Network error - unable to reach API"
✅ Expected: Retry logic then fallback
✅ User Experience: Brief delay then normal operation
```

---

## 🎪 EXPECTED TEST RESULTS

### ✅ **Success Scenarios**
- All mock tests should pass consistently
- Daily usage tracking should increment correctly
- Rate limiting should enforce delays properly
- Fallback should activate when AI unavailable
- Performance should be acceptable (Bogus < 10s, AI < 30s)

### ⚠️ **Acceptable "Failures"**
- Database connection errors (without real DB)
- Real API quota exceeded (in production environment)
- Network timeouts (environmental issues)

### ❌ **Unacceptable Failures**
- Mock services not working properly
- Fallback logic not activating
- Application crashes on API limits
- Incorrect usage statistics

---

## 🔧 TROUBLESHOOTING GUIDE

### **Problem**: Tests fail with "No API key"
**Solution**: Configure API key in `appsettings.json` or environment variable
```json
{
  "GeminiApiKey": "your-actual-api-key-here"
}
```

### **Problem**: Tests timeout
**Solution**: Check network connection and API endpoint availability

### **Problem**: All API tests fail
**Solution**: 
1. Verify API key validity
2. Check if quota exhausted
3. Test with mock services first

### **Problem**: Fallback not working
**Solution**:
1. Check `UseAI` parameter handling
2. Verify Bogus generation still works
3. Review exception handling logic

---

## 📈 CONTINUOUS IMPROVEMENT

### **Monitoring Recommendations**:
1. **Daily Usage Alerts**: Set up alerts at 80% quota usage
2. **Performance Tracking**: Monitor AI vs Bogus generation times  
3. **Error Rate Monitoring**: Track API failure rates
4. **Fallback Success Rate**: Ensure fallback always works

### **Future Enhancements**:
1. **Adaptive Rate Limiting**: Adjust call frequency based on quota
2. **Smart Quota Distribution**: Prioritize important data generation
3. **Multi-API Support**: Fallback to other AI providers
4. **Caching**: Reduce API calls for similar prompts

---

## 🎉 SUCCESS CRITERIA

✅ **All API limit scenarios tested comprehensively**  
✅ **Fallback behavior working in all conditions**  
✅ **Mock services validate logic correctly**  
✅ **Real API integration stable with proper limits**  
✅ **Performance acceptable in all scenarios**  
✅ **User experience smooth regardless of API status**  

---

## 🔗 RELATED FILES

- `/SqlTestDataGenerator.Tests/APILimitTestSuite.cs` - Main test suite
- `/SqlTestDataGenerator.Tests/MockAPILimitService.cs` - Mock services
- `/SqlTestDataGenerator.Tests/APILimitFallbackTests.cs` - Fallback tests
- `/scripts/test-api-limits.ps1` - Automated testing script
- `/SqlTestDataGenerator.Core/Services/EnhancedGeminiFlashRotationService.cs` - Core API service

**Task Status**: ✅ **COMPLETED**  
**Next Steps**: Run tests regularly và monitor API usage in production 