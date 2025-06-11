# Task Summary: Fix Truncate Data Trước Khi Insert Tables

## 🎯 **Vấn đề Ban Đầu**
User yêu cầu sửa code để truncate data trước khi insert các table để tránh duplicate key errors.

**Symptom**: 
- TC001 test failed với error: `Duplicate entry 'DD_MANAGER' for key 'roles.code'`
- Engine đã truncate tables (deleted 0 rows) nhưng vẫn có duplicate keys

## 🔧 **Solutions Đã Implement**

### 1. **Enhanced Truncate Logic** (✅ COMPLETED)
**File**: `SqlTestDataGenerator.Core/Services/EngineService.cs`

**Cải tiến**:
- **Atomic Transaction**: Bọc toàn bộ truncate process trong transaction
- **Comprehensive Deletion**: Sử dụng `DELETE` thay vì `TRUNCATE` để ensure hoàn toàn xóa data  
- **FK Safety**: Disable foreign key checks (MySQL) trước khi truncate
- **Auto-increment Reset**: Reset auto-increment counters về 1
- **Dependency Order**: Truncate child tables trước parent tables

**Key Changes**:
```csharp
// Phase 1: Delete all data from tables
var deleteCommand = "DELETE FROM `{table.TableName}`";
var deletedRows = await connection.ExecuteAsync(deleteCommand, transaction: transaction);

// Phase 2: Reset auto-increment/identity counters  
var resetCommand = "ALTER TABLE `{table.TableName}` AUTO_INCREMENT = 1";
await connection.ExecuteAsync(resetCommand, transaction: transaction);

// Phase 3: Commit truncation transaction
transaction.Commit();
```

### 2. **Unique Code Generation Enhancement** (🔄 PARTIALLY COMPLETED)
**Files**: 
- `SqlTestDataGenerator.Core/Services/DataGenService.cs` (GenerateStringValue)
- `SqlTestDataGenerator.Core/Services/DataGenService.cs` (GenerateLikeValue)

**Cải tiến**:
- **GUID + Timestamp**: Sử dụng timestamp + recordIndex cho absolute uniqueness
- **Fallback Strategy**: Nếu quá dài, fallback về shorter GUID approach
- **Multi-Path Coverage**: Fix cả GenerateStringValue và GenerateLikeValue methods

**Key Changes**:
```csharp
// Create absolutely unique code using timestamp + recordIndex
var uniqueSuffix = $"{DateTime.Now.Ticks % 10000:D4}_{recordIndex:D3}";
value = $"{baseCode}_{uniqueSuffix}";
```

## 📊 **Test Results**

### ✅ **Truncate Logic**: WORKING 
- Enhanced truncation hoạt động correctly
- Tables được clean thành công (0 rows deleted = tables đã empty)
- Auto-increment reset thành công

### ❌ **Unique Generation**: STILL HAVING ISSUES
- Test vẫn fail với duplicate keys: `'DD_MANAGER'`, `'DD_LEAD'`
- Code vẫn generate plain values without unique suffixes
- Có thể có additional paths generating data chưa được fix

## 🔍 **Root Cause Analysis**

**Why duplicate keys persist?**

1. **Multiple Generation Paths**: Có thể có paths khác trong code (như CoordinatedDataGenerator) chưa được update
2. **Conditional Logic**: Logic unique generation có thể bị bypass trong certain conditions  
3. **Caching Issues**: Có thể old compiled code vẫn đang được sử dụng
4. **SQL Requirements Override**: Logic SQL requirements có thể override unique generation

## 📋 **Next Steps Recommended**

### 🎯 **Immediate Actions**
1. **Investigate All Generation Paths**:
   - Check `CoordinatedDataGenerator.cs` 
   - Check SQL requirements logic override
   - Find all places generating `roles.code`

2. **Add Debug Logging**:
   ```csharp
   Console.WriteLine($"[DEBUG] Generated roles.code: {value} for recordIndex: {recordIndex}");
   ```

3. **Force Unique Override**:
   ```csharp
   // Force absolutely unique for roles.code regardless of other logic
   if (tableName.ToLower() == "roles" && columnName == "code") {
       value = $"ROLE_{Guid.NewGuid().ToString("N")[..8]}_{recordIndex:D3}";
   }
   ```

### 🎯 **Alternative Solutions**

**Option 1: Simple GUID Approach**
```csharp
if (tableName.ToLower() == "roles" && columnName == "code") {
    value = $"ROLE_{Guid.NewGuid().ToString("N")[..12]}";  // Always unique
}
```

**Option 2: Database-level Unique Constraint Handling**
- Check if MySQL has proper unique constraints set
- Consider using `INSERT IGNORE` or `ON DUPLICATE KEY UPDATE`

**Option 3: Pre-execution Unique Validation**
- Validate generated INSERT statements for duplicates before execution
- De-duplicate trong memory trước khi send to database

## 🎉 **Success Metrics**

**✅ Completed**:
- Enhanced truncate transaction logic
- Multi-path unique code generation updates
- Comprehensive logging và error handling

**🔄 In Progress**:
- Full unique code generation coverage
- Eliminate all duplicate key possibilities

**🎯 Target**:
- TC001 test passes consistently
- No duplicate entry errors
- Clean data generation workflow

## 📚 **Files Modified**

1. **SqlTestDataGenerator.Core/Services/EngineService.cs**
   - Enhanced `TruncateTablesAsync` method
   - Added transaction-based truncation
   - Fixed tables.Reverse() compilation error

2. **SqlTestDataGenerator.Core/Services/DataGenService.cs**  
   - Updated `GenerateStringValue` unique code logic
   - Updated `GenerateLikeValue` unique code logic
   - Added GUID + timestamp approach

## 💡 **Lessons Learned**

1. **Complex Systems Need Comprehensive Fixes**: Multiple generation paths require coordinated updates
2. **Test-driven Debugging**: Real tests reveal issues that might be missed in isolation
3. **Unique Key Generation**: GUID-based approaches are more reliable than deterministic ones for absolute uniqueness
4. **Transaction Safety**: Database operations need proper transaction handling for atomicity

## 🚀 **Current Status**: 
**PARTIALLY COMPLETED** - Truncate logic fixed, unique generation needs final touches 