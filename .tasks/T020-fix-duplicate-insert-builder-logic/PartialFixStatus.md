# Task T020: Partial Fix Status - Duplicate INSERT Builder Logic

## 🎯 **Current Status: PARTIALLY COMPLETED**

### ✅ **What Was Successfully Completed**
1. **CommonInsertBuilder Service Created** ✅
   - Added proper generated column filtering
   - Supports both database record + column list approaches
   - Includes validation methods
   - Has proper logging

2. **CoordinatedDataGenerator Refactored** ✅  
   - Now uses CommonInsertBuilder.BuildInsertStatement()
   - Includes validation for generated columns
   - Removed duplicate code (BuildInsertStatement, QuoteIdentifier, FormatValue)

3. **DataGenService Partially Refactored** ⚠️
   - Uses CommonInsertBuilder.GetInsertableColumns() for filtering
   - Uses CommonInsertBuilder.BuildInsertStatement() for SQL generation
   - Removed QuoteIdentifier method

### ❌ **What Is Still Failing** 

#### Issue: TC001 vẫn fail với same error
```
The value specified for generated column 'full_name' in table 'users' is not allowed.
```

#### Root Cause Analysis
Từ test logs:
- **Companies & Roles**: ✅ Execute thành công (29/60 statements)
- **Users table**: ❌ Vẫn include generated columns

#### Possible Reasons For Continued Failure
1. **Query Classification**: TC001 có thể không được classify as complex query
2. **Fallback Path**: Engine có thể use fallback path chưa được refactor
3. **Record Generation**: Issue trong việc tạo record data (before INSERT building)
4. **Incomplete Refactor**: Có path nào đó vẫn chưa dùng CommonInsertBuilder

### 🔍 **Evidence From Logs**
```
[EngineService] Generated 60 INSERT statements
[EngineService] Reordered INSERT statements by dependencies
```
**Missing**: Không có logs từ CommonInsertBuilder → Indicating old path vẫn được dùng

### 🚨 **Critical Finding**
`full_name` vẫn xuất hiện trong INSERT statement:
```sql
INSERT INTO `users` (`username`, `email`, `password_hash`, `first_name`, `last_name`, `full_name`, ...)
```

### 📋 **Next Steps Required**
1. **Debug Query Classification**: Check if TC001 triggers CoordinatedDataGenerator hay DataGenService
2. **Add More Logging**: Trace exact path being taken
3. **Complete DataGenService Refactor**: Ensure tất cả paths dùng CommonInsertBuilder  
4. **Validation**: Add runtime validation to catch generated columns in INSERT

### 🎯 **Impact Assessment**
- **Architecture**: ✅ CommonInsertBuilder foundation is solid
- **CoordinatedDataGenerator**: ✅ Fully refactored and should work correctly
- **DataGenService**: ⚠️ Needs additional investigation and potential fixes
- **Test Results**: ❌ Still failing, requires deeper debugging

## **Conclusion**
The duplicate code issue has been architecturally resolved with CommonInsertBuilder, but there's still a code path that's not using the new architecture. This requires investigation into which specific generator is being triggered by TC001 and why it's still including generated columns.

**Next Action**: Debug the actual execution path in TC001 to identify the remaining issue. 