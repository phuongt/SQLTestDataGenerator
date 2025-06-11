# Generated Column Issue in INSERT Builder

## Tóm tắt Issue
INSERT statement builder vẫn include generated columns trong SQL statement mặc dù đã detect và set `IsGenerated=TRUE`.

## Cách Reproduce
1. Kết nối MySQL database với table có STORED GENERATED column (ví dụ: `full_name`)
2. Chạy `ExecuteQueryWithTestDataAsync` với query JOIN table đó
3. Engine sẽ generate INSERT statement bao gồm generated column
4. MySQL sẽ reject với error: `The value specified for generated column 'full_name' in table 'users' is not allowed.`

## Root Cause
- ✅ `SqlMetadataService` correctly detects generated columns
- ✅ `IsGenerated=TRUE` flag được set correctly  
- ❌ INSERT statement builder ignores `IsGenerated` flag và vẫn include column

## Solution/Workaround
**IN INSERT BUILDER** - Filter out columns với `IsGenerated=TRUE`:

```csharp
// BEFORE (wrong)
var columns = tableSchema.Columns.ToList();

// AFTER (correct)  
var columns = tableSchema.Columns.Where(c => !c.IsGenerated).ToList();
```

## Related Commit/Task
- Task: T019-retest-case-001-complex-vowis-sql
- Test Case: TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithRealMySQL
- Error First Seen: 2025-01-17

## Impact
- **Severity**: CRITICAL
- **Scope**: All MySQL tables với generated columns
- **Blocking**: Prevents any INSERT operation on affected tables

## Detection Log Pattern
```
[SqlMetadataService] Found generated column: full_name with EXTRA: STORED GENERATED, IS_GENERATED: 1 
[SqlMetadataService] SET IsGenerated=TRUE for column: full_name
[EngineService] INSERT failed: The value specified for generated column 'full_name' in table 'users' is not allowed.
```

## Example Problem Statement
```sql
-- ❌ WRONG: Includes generated column
INSERT INTO `users` (`username`, `email`, `first_name`, `last_name`, `full_name`, ...) 
VALUES ('user1', 'test@email.com', 'John', 'Doe', 'Generated Value', ...)

-- ✅ CORRECT: Excludes generated column  
INSERT INTO `users` (`username`, `email`, `first_name`, `last_name`, ...) 
VALUES ('user1', 'test@email.com', 'John', 'Doe', ...)
``` 