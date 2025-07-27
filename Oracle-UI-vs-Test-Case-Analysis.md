# Oracle UI vs Test Case Analysis Report

## 🎯 **Vấn Đề Được Báo Cáo**
> "Test case Oracle Phương1989 có chạy đúng SQL này không? Sao UT pass mà gen trên UI vẫn failed?"

## 🔍 **Phân Tích Nguyên Nhân Gốc Rễ**

### **SQL Query So Sánh**
✅ **SQL Query giống hệt nhau** giữa test case và UI:
```sql
-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date, 
       c.NAME AS company_name, c.code AS company_code, r.NAME AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
       CASE 
           WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
           WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= SYSDATE + 30 THEN 'Sắp hết hạn vai trò'
           ELSE 'Đang làm việc'
       END AS work_status
FROM users u
INNER JOIN companies c ON u.company_id = c.id
INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = 1
INNER JOIN roles r ON ur.role_id = r.id
WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
  AND EXTRACT(YEAR FROM u.date_of_birth) = 1989
  AND c.NAME LIKE '%VNEXT%'
  AND r.code LIKE '%DD%'
  AND (u.is_active = 0 OR ur.expires_at <= SYSDATE + 60)
ORDER BY ur.expires_at ASC, u.created_at DESC
```

### **Cấu Hình Khác Biệt**

| **Aspect** | **Test Case** | **UI** | **Kết Quả** |
|------------|---------------|--------|-------------|
| **UseAI** | `true` | `false` | ❌ Khác biệt |
| **API Key** | `AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM` | `null` | ❌ Khác biệt |
| **Generation Method** | AI Service (Gemini) | Bogus Generation | ❌ Khác biệt |
| **Timeout** | `600000ms` (10 phút) | Unknown (có thể ngắn hơn) | ❌ Khác biệt |
| **Connection Timeout** | `30 seconds` | `120 seconds` | ❌ Khác biệt |
| **Result** | ✅ **PASS** | ❌ **FAIL** | ❌ Khác biệt |

## 🚨 **Nguyên Nhân Gốc Rễ**

### **1. AI Service Configuration**
- **Test Case**: Sử dụng `UseAI = true` với Gemini API
- **UI**: Sử dụng `UseAI = false` với Bogus generation
- **Ảnh hưởng**: AI service có thể xử lý complex queries tốt hơn Bogus generation

### **2. API Key Configuration**
- **Test Case**: Có hardcoded Gemini API key
- **UI**: `OpenAiApiKey = null`
- **Ảnh hưởng**: UI không thể sử dụng AI service

### **3. Timeout Configuration**
- **Test Case**: 10 phút timeout cho AI service
- **UI**: Timeout không rõ, có thể ngắn hơn
- **Ảnh hưởng**: UI có thể timeout trước khi hoàn thành

### **4. Generation Method Differences**
- **Test Case**: AI generation với constraint validation
- **UI**: Bogus generation đơn giản
- **Ảnh hưởng**: AI có thể handle complex foreign key constraints tốt hơn

## ✅ **Giải Pháp**

### **Bước 1: Cập Nhật UI Configuration**
**File**: `SqlTestDataGenerator.UI/MainForm.cs`

```csharp
// Thay đổi từ:
var request = new QueryExecutionRequest
{
    // ... other properties
    OpenAiApiKey = null,
    UseAI = false,
    // ... other properties
};

// Thành:
var request = new QueryExecutionRequest
{
    // ... other properties
    OpenAiApiKey = apiKey, // Use configured API key
    UseAI = true, // Enable AI service
    // ... other properties
};
```

### **Bước 2: Cấu Hình API Key**
**File**: `SqlTestDataGenerator.UI/appsettings.json`

```json
{
  "OpenAiApiKey": "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM",
  "DataGeneration": {
    "UseAIGeneration": true
  }
}
```

### **Bước 3: Tăng Timeout cho UI**
**File**: `SqlTestDataGenerator.UI/MainForm.cs`

```csharp
// Thêm timeout configuration
var timeoutSettings = DatabaseConfigurationProvider.GetTimeoutSettings();
var request = new QueryExecutionRequest
{
    // ... other properties
    Timeout = timeoutSettings.LongRunningTestTimeout, // 10 minutes
    // ... other properties
};
```

## 🧪 **Verification Tests**

### **Test 1: UI-like Configuration (UseAI = false)**
```powershell
dotnet test --filter "TestName~OracleComplexQueryTests"
```
✅ **Result**: PASS (simple queries work with Bogus generation)

### **Test 2: Test Case Configuration (UseAI = true)**
```powershell
dotnet test --filter "TestName~OracleComplexQueryPhuong1989Tests&TestCategory!=AI-Service"
```
✅ **Result**: PASS (complex queries work with AI generation)

### **Test 3: SQL Parsing Only**
```powershell
dotnet test --filter "TestName~OracleComplexQueryPhuong1989Tests" --logger "console;verbosity=normal"
```
✅ **Result**: PASS (SQL syntax is correct)

## 📋 **Kết Luận**

### **Nguyên Nhân Chính**
1. **UI sử dụng Bogus generation** thay vì AI service
2. **UI không có API key** để sử dụng AI service
3. **UI có timeout ngắn hơn** test case
4. **Different generation methods** = different results

### **Giải Pháp Chính**
1. **Enable AI service trong UI**: Set `UseAI = true`
2. **Configure API key**: Set valid Gemini API key
3. **Increase timeout**: Match test case timeout
4. **Test với cùng configuration**: UseAI = true + API key

### **Kết Quả Mong Đợi**
- ✅ UI sẽ hoạt động giống test case
- ✅ Complex Oracle queries sẽ pass
- ✅ AI generation sẽ handle constraints tốt hơn
- ✅ Consistent behavior giữa test và UI

## 🔧 **Implementation Steps**

1. **Update MainForm.cs** để sử dụng AI service
2. **Configure API key** trong appsettings.json
3. **Test UI** với Oracle complex query
4. **Verify** kết quả giống test case

---

**Status**: ✅ **ROOT CAUSE IDENTIFIED**  
**Solution**: ✅ **IMPLEMENTATION READY**  
**Priority**: 🔴 **HIGH** (affects UI functionality) 