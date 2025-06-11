# DB-MySQL-Major-json-column-generation

## Tóm tắt vấn đề
SqlTestDataGenerator fails khi generate data cho MySQL JSON columns vì không có specific handling cho JSON data type.

## Cách tái tạo
1. Kết nối tới MySQL database có table với JSON column (ví dụ: `roles.permissions JSON`)
2. Chạy query có JOIN với table đó
3. Data generation fails với error: `Invalid JSON text: "Invalid value." at position 0`

## Root Cause Analysis

**Issue:** Method `GenerateBogusValue()` không có case handling cho JSON data type, nên fallback về `GenerateDefaultValue()` và tạo string `'DefaultValue_1'` - không phải valid JSON format.

**Error Message:**
```
Invalid JSON text: "Invalid value." at position 0 in value for column 'roles.permissions'.
SQL: INSERT INTO `roles` (..., `permissions`, ...) VALUES (..., 'DefaultValue_1', ...)
```

**Root Cause:** 
- JSON columns yêu cầu valid JSON format
- System thiếu "json" case trong data type switch statement
- Không có `GenerateJsonValue()` method

## Solution/Workaround

**Fix Applied:**

1. **Added JSON case trong GenerateBogusValue():**
```csharp
"json" or "jsonb" => GenerateJsonValue(faker, column, recordIndex),
```

2. **Added GenerateJsonValue() method:**
- `permissions` columns → Generate realistic permissions array: `'["read","write","admin"]'`
- `config/settings` columns → Generate configuration object: `'{"theme":"light","notifications":true}'`
- `metadata/data/info` columns → Generate generic metadata object
- Default case → Empty JSON object: `'{}'`

**Code Changes:**
- File: `SqlTestDataGenerator.Core/Services/DataGenService.cs`
- Lines: ~120 (switch statement), ~265+ (new method)

## Test Results
**Before Fix:** 
- 3-4 table JOIN queries failed với JSON error
- Only 2-table JOIN worked

**After Fix:**
- All complex JOIN queries should work
- JSON columns generate valid JSON data
- Proper permissions arrays for role-based queries

## Related Task
- Task ID: T005-unit-test-generate-data-function
- Complex query testing với multi-table JOINs 