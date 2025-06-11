# SqlQueryParser WHERE Conditions Not Extracted

## Summary
SqlQueryParser.ParseQuery() không extract được WHERE conditions, dẫn đến tất cả unit tests fail với Expected: 1, Actual: 0.

## Error Details
- **Component**: `SqlTestDataGenerator.Core.Services.SqlQueryParser`
- **Method**: `ExtractWhereConditions()`
- **Issue**: Regex pattern lỗi và WHERE clause không được phát hiện
- **Log Evidence**: Không có log nào từ SqlQueryParser, chỉ có log từ SqlMetadataService

## How to Reproduce
1. Tạo SqlQueryParser instance
2. Gọi `ParseQuery("SELECT * FROM users WHERE status = 'active'")`
3. Check `result.WhereConditions.Count` - sẽ bằng 0 thay vì 1

## Root Cause Analysis

### Problem 1: Invalid Regex Character Class
```csharp
// BEFORE (LỖI):
var equalityPattern = @"(\w+\.)?(\w+)\s*=\s*(['""]?)([^'"";\s)]+)\3";
//                                                      ^ Lỗi: closing ] trong character class

// AFTER (FIXED):
var equalityPattern = @"(\w+\.)?(\w+)\s*=\s*(['""]?)([^'"";\s\)]+)\3";
//                                                      ^ Escaped properly
```

### Problem 2: WHERE Clause Detection Issue
- WHERE regex pattern có thể không match đúng
- Logger setup có thể chưa đúng
- Method không được gọi hoặc fail silently

## Fixed Issues
✅ **Regex Character Class**: Fixed trong:
- `ParseEqualityConditions()` line 136
- `ParseComparisonConditions()` line 224  
- `ExtractJoinRequirements()` line 318

## Testing Status
❌ **Unit Tests Still Failing**: All 10 tests fail với 0 conditions extracted

## Workaround
Tạm thời có thể bypass SqlQueryParser nếu cần hoặc sử dụng trực tiếp regex patterns đã fix.

## Fix Implementation

### File: SqlQueryParser.cs
```csharp
// Line 136 - ParseEqualityConditions
var equalityPattern = @"(\w+\.)?(\w+)\s*=\s*(['""]?)([^'"";\s\)]+)\3";

// Line 224 - ParseComparisonConditions  
var comparisonPattern = @"(\w+\.)?(\w+)\s*(>=|<=|>|<)\s*(['""]?)([^'"";\s\)]+)\4";

// Line 318 - ExtractJoinRequirements
var joinPattern = @"(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+)?JOIN\s+(\w+)\s+(\w+)\s+ON\s+([^()]+?)(?=\s+(?:INNER|LEFT|RIGHT|FULL|WHERE|ORDER|GROUP|LIMIT|$))";
```

## Debug Actions Needed
1. ❌ Add debug logging trong ExtractWhereConditions
2. ❌ Test WHERE clause regex riêng biệt
3. ❌ Verify logger configuration trong SqlQueryParser
4. ❌ Check if ExtractWhereConditions() được gọi

## Related Files
- `SqlTestDataGenerator.Core/Services/SqlQueryParser.cs`
- `SqlTestDataGenerator.Tests/SqlQueryParserTests.cs`
- Task: `T025-continue-after-connection-error`

## Status
- **Severity**: Critical (affects SQL parsing functionality)
- **Frequency**: Always (100% test failure rate)
- **Impact**: High (blocks WHERE condition parsing)
- **Fixed Patterns**: ✅ 3/3 regex patterns
- **Tests Status**: ❌ Still failing
- **Last Updated**: 2024-12-07 