# DB-Oracle-Critical-select-1-syntax-error

## Severity: Critical
## Category: Database Connection
## Database: Oracle
## Status: ✅ RESOLVED

## Problem Description
Khi test connection với Oracle database, ứng dụng báo lỗi `ORA-00923: FROMキーワードが指定の位置にありません` (FROM keyword not found where expected) mặc dù Oracle server đang chạy bình thường.

## Root Cause
Oracle không hỗ trợ cú pháp `SELECT 1` mà yêu cầu phải có FROM clause. Oracle sử dụng bảng DUAL cho các queries đơn giản.

## Error Details
```
Oracle.ManagedDataAccess.Client.OracleException (0x80004005): ORA-00923: FROMキーワードが指定の位置にありません。
https://docs.oracle.com/error-help/db/ora-00923/
```

## Affected Code
**File**: `SqlTestDataGenerator.Core/Services/EngineService.cs`
**Method**: `TestConnectionAsync`
**Lines**: 103-107

### Before (Broken)
```csharp
var testQuery = databaseType.ToLower() switch
{
    "sql server" => "SELECT 1",
    "mysql" => "SELECT 1",
    "postgresql" => "SELECT 1",
    _ => "SELECT 1"  // ❌ Oracle falls through to this
};
```

### After (Fixed)
```csharp
var testQuery = databaseType.ToLower() switch
{
    "sql server" => "SELECT 1",
    "mysql" => "SELECT 1",
    "postgresql" => "SELECT 1",
    "oracle" => "SELECT 1 FROM DUAL",  // ✅ Added Oracle case
    _ => "SELECT 1"
};
```

## Solution
1. Thêm case riêng cho Oracle trong switch statement
2. Sử dụng `SELECT 1 FROM DUAL` thay vì `SELECT 1`
3. Test với Oracle connection để verify fix

## Prevention
- Luôn test tất cả database types khi thêm connection logic
- Hiểu rõ cú pháp SQL khác nhau giữa các database
- Oracle yêu cầu FROM clause, sử dụng DUAL table cho queries đơn giản

## Related Files
- `SqlTestDataGenerator.Core/Services/EngineService.cs`
- `scripts/test-oracle-connection.ps1` (test script)
- `scripts/test-oracle-connection-diagnostic.ps1` (diagnostic script)
- `scripts/test-oracle-connection-quick.ps1` (quick test script)

## Resolution Details
**Date Fixed**: 2025-07-24
**Fix Applied**: Added Oracle case in TestConnectionAsync method
**Verification**: All Oracle connection tests now pass
**Impact**: Oracle connection test in UI now works correctly

## Status: ✅ RESOLVED 