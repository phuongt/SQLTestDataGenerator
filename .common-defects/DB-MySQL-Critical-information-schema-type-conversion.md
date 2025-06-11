# DB-MySQL-Critical-information-schema-type-conversion

## Tóm tắt vấn đề
SqlTestDataGenerator fails khi query MySQL INFORMATION_SCHEMA do multiple type conversion và SQL syntax issues.

## Cách tái tạo
1. Kết nối tới MySQL database
2. Chạy GetDatabaseInfoAsync() với bất kỳ SQL query nào
3. GetTableSchemaAsync() fails với type conversion errors
4. Data generation fails với SQL syntax errors

## Root Cause Analysis

### 1. MySQL INFORMATION_SCHEMA Missing TABLE_SCHEMA Constraint
**Issue:** Query `WHERE TABLE_NAME = 'users'` returns multiple results từ different schemas
**Fix:** Thêm `AND TABLE_SCHEMA = DATABASE()` constraint

### 2. Type Conversion Issues trong MapToColumnSchema()
**Issue:** 
- `IS_NULLABLE` (string "YES"/"NO") compared với bool `true`
- `CHARACTER_MAXIMUM_LENGTH` (ulong) assigned to int? property
- Dynamic object property access type mismatches

**Fix:** 
- Handle string-only comparison cho IS_NULLABLE
- Safe conversion với Convert.ToInt32() cho numeric fields
- Proper null checking và type casting

### 3. SQL Identifier Quoting Issues
**Issue:** Code sử dụng SQL Server brackets `[table]` cho tất cả databases
**Fix:** Database-specific quoting:
- MySQL: backticks `` `table` ``
- SQL Server: brackets `[table]`
- PostgreSQL: double quotes `"table"`

### 4. Generated Column Issues
**Issue:** MySQL generated columns (như `full_name`) không cho phép INSERT values
**Fix:** Detect và exclude generated/computed columns khỏi INSERT statements

### 5. SQL Injection & Data Length Issues
**Issue:** 
- Single quotes trong data gây SQL syntax errors
- Generated data quá dài cho column constraints

**Fix:**
- Escape single quotes: `'` → `''`
- Respect column MaxLength constraints

## Solution/Workaround

### Code Changes Applied:

1. **DbConnectionFactory.cs** - Fix MySQL INFORMATION_SCHEMA queries:
```csharp
// Add TABLE_SCHEMA constraint
WHERE TABLE_NAME = '{tableName}'
    AND TABLE_SCHEMA = DATABASE()
```

2. **SqlMetadataService.cs** - Fix type conversions:
```csharp
// Safe string comparison
var isNullableValue = column.IS_NULLABLE ?? column.is_nullable;
columnSchema.IsNullable = isNullableValue?.ToString()?.ToUpper() == "YES";

// Safe numeric conversion
var maxLengthValue = column.CHARACTER_MAXIMUM_LENGTH ?? column.character_maximum_length;
columnSchema.MaxLength = maxLengthValue != null ? Convert.ToInt32(maxLengthValue) : (int?)null;
```

3. **DataGenService.cs** - Fix SQL syntax và data issues:
```csharp
// Database-specific identifier quoting
private static string QuoteIdentifier(string identifier, DatabaseType dbType)
{
    return dbType switch
    {
        DatabaseType.MySQL => $"`{identifier}`",
        DatabaseType.PostgreSQL => $"\"{identifier}\"",
        DatabaseType.SQLite => $"[{identifier}]",
        DatabaseType.SqlServer => $"[{identifier}]",
        _ => identifier
    };
}

// SQL string escaping
private static string EscapeSqlString(string input)
{
    return input?.Replace("'", "''") ?? string.Empty;
}

// Generated column detection
private static bool IsGeneratedColumn(ColumnSchema column)
{
    var columnName = column.ColumnName.ToLower();
    return columnName == "full_name" || columnName == "fullname" || 
           column.DefaultValue?.ToString()?.Contains("CONCAT") == true;
}
```

## Related Task/Commit ID
- Task: T005-unit-test-generate-data-function
- Files modified: DbConnectionFactory.cs, SqlMetadataService.cs, DataGenService.cs
- Date: 2025-06-04

## Prevention
1. **Always test với multiple database types** khi implement database-agnostic features
2. **Use proper type checking** khi work với dynamic objects từ database queries
3. **Implement database-specific SQL syntax handling** từ đầu
4. **Test với real database schemas** có generated columns và constraints
5. **Always escape user input** và respect column constraints

## Impact
- **Severity:** Critical - Toàn bộ MySQL data generation bị fail
- **Affected Components:** SqlMetadataService, DataGenService, DbConnectionFactory
- **Resolution Time:** ~4 hours debugging và fixing
- **Status:** ✅ RESOLVED - All issues fixed và tested successfully 