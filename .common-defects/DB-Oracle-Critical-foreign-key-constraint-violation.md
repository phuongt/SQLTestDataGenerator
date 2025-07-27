# DB-Oracle-Critical-foreign-key-constraint-violation

## Severity: Critical
## Category: Database Foreign Key Constraints
## Database: Oracle
## Status: ✅ RESOLVED

## Problem Description
Khi thực thi INSERT statements với Oracle database, ứng dụng báo lỗi `ORA-02291: 整合性制約(SYSTEM.FK_USERS_ROLE)に違反しました - 親キーがありません` (Foreign key constraint violation - parent key does not exist) mặc dù dependency order đã đúng và foreign key values hợp lệ.

## Root Cause
Oracle không hỗ trợ `SET FOREIGN_KEY_CHECKS` như MySQL. Oracle enforces foreign key constraints ngay lập tức và không cho phép disable chúng. Vấn đề xảy ra khi:

1. **Transaction Handling**: Oracle yêu cầu parent records phải được commit trước khi child records có thể reference chúng
2. **Execution Order**: Mặc dù dependency resolver đã sắp xếp đúng thứ tự, nhưng transaction rollback có thể gây ra vấn đề
3. **Constraint Enforcement**: Oracle kiểm tra foreign key constraints ngay khi INSERT, không như MySQL có thể disable tạm thời

## Error Details
```
ORA-02291: 整合性制約(SYSTEM.FK_USERS_ROLE)に違反しました - 親キーがありません
https://docs.oracle.com/error-help/db/ora-02291/
SQL: INSERT INTO users (USERNAME, EMAIL, PASSWORD_HASH, FIRST_NAME, LAST_NAME, PHONE, ADDRESS, DATE_OF_BIRTH, GENDER, AVATAR_URL, COMPANY_ID, PRIMARY_ROLE_ID, SALARY, HIRE_DATE, DEPARTMENT, IS_ACTIVE, LAST_LOGIN_AT, CREATED_AT, UPDATED_AT) VALUES ('''Value_1''', '''Value_1''', '''Value_1''', '''Phương_001_qe5''', '''Phương_001_f7e''', '''Value_1''', '''Value_1''', TO_DATE('1977-04-02', 'YYYY-MM-DD'), '''Value_1''', '''Value_1''', 2, 2, 1, TO_DATE('2025-02-09', 'YYYY-MM-DD'), '''Value_1''', 0, TO_TIMESTAMP('2025-07-27 05:48:45', 'YYYY-MM-DD HH24:MI:SS'), TO_TIMESTAMP('2025-07-27 05:48:45', 'YYYY-MM-DD HH24:MI:SS'), TO_TIMESTAMP('2025-07-27 05:48:45', 'YYYY-MM-DD HH24:MI:SS'))
```

## Analysis Results
- ✅ **Dependency Order**: Correct (roles → companies → users → user_roles)
- ✅ **Foreign Key Values**: Valid (PRIMARY_ROLE_ID = 2, role ID 2 exists)
- ✅ **Generated SQL**: Properly formatted for Oracle
- ❌ **Transaction Handling**: Single transaction with rollback causes FK constraint issues

## Solution Applied

### Files Modified
- `SqlTestDataGenerator.Core/Services/EngineService.cs`
- `SqlTestDataGenerator.Tests/CompleteWorkflowAutomatedTest.cs`

### Implementation Details

#### 1. **Oracle-Specific Transaction Handling**
```csharp
// 🔧 CRITICAL FIX: Oracle-specific transaction handling
if (request.DatabaseType.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
{
    // For Oracle, we need to commit each table separately to ensure FK constraints are satisfied
    await ExecuteOracleInsertsWithTableCommits(connection, insertStatements, databaseInfo, result, request);
}
else
{
    // For other databases, use single transaction
    using var transaction = connection.BeginTransaction();
    try
    {
        await ExecuteInsertsInTransaction(connection, transaction, insertStatements, databaseInfo, result, request);
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

#### 2. **Table-Specific Commits for Oracle**
```csharp
/// <summary>
/// Execute INSERT statements for Oracle with table-specific commits to satisfy FK constraints.
/// </summary>
private async Task ExecuteOracleInsertsWithTableCommits(IDbConnection connection, List<InsertStatement> insertStatements, DatabaseInfo databaseInfo, QueryExecutionResult result, QueryExecutionRequest request)
{
    // Process tables in dependency order (parents first, children last)
    var tableExecutionOrder = _dependencyResolver.OrderTablesByDependencies(
        statementsByTable.Keys.ToList(), databaseInfo);
    
    // Insert ALL statements for each table before moving to next table
    foreach (var currentTable in tableExecutionOrder)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            foreach (var insert in tableStatements)
            {
                await connection.ExecuteAsync(insert.SqlStatement, transaction: transaction, commandTimeout: 300);
            }
            transaction.Commit();
            Console.WriteLine($"[EngineService] Oracle: Committed table {currentTable} successfully");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw;
        }
    }
}
```

#### 3. **Enhanced Error Handling**
```csharp
// 🔧 CRITICAL FIX: For Oracle, provide more specific error information
if (request.DatabaseType.Equals("Oracle", StringComparison.OrdinalIgnoreCase) && 
    insertEx.Message.Contains("ORA-02291"))
{
    Console.WriteLine($"[EngineService] Oracle Foreign Key Constraint Violation detected");
    Console.WriteLine($"[EngineService] This suggests a dependency order issue or missing parent record");
    Console.WriteLine($"[EngineService] Current table: {currentTable}");
    Console.WriteLine($"[EngineService] Execution order: {string.Join(" → ", tableExecutionOrder)}");
}
```

#### 4. **Fixed Test Case**
```csharp
// Oracle doesn't support SET FOREIGN_KEY_CHECKS - rely on dependency order
// The generated SQL should already be in correct dependency order

foreach (var statement in sqlStatements)
{
    if (statement.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
    {
        await connection.ExecuteAsync(statement, transaction: transaction);
    }
}
```

## Verification

### Test Results
- ✅ **Dependency Order**: Verified correct (roles → companies → users → user_roles)
- ✅ **Foreign Key Values**: Verified valid (PRIMARY_ROLE_ID = 2, role ID 2 exists)
- ✅ **Generated SQL**: Verified properly formatted for Oracle
- ✅ **Transaction Handling**: Fixed with table-specific commits

### Generated SQL Analysis
```sql
-- Correct dependency order:
1. INSERT INTO roles (...) VALUES (...) -- Parent table
2. INSERT INTO companies (...) VALUES (...) -- Parent table  
3. INSERT INTO users (...) VALUES (...) -- Child table (depends on roles, companies)
4. INSERT INTO user_roles (...) VALUES (...) -- Junction table (depends on users, roles)
```

## Prevention
- **Oracle-Specific Handling**: Always use table-specific commits for Oracle foreign key constraints
- **Dependency Resolution**: Ensure EnhancedDependencyResolver is used for all database types
- **Transaction Management**: Use appropriate transaction strategy based on database type
- **Error Handling**: Provide specific error messages for Oracle constraint violations

## Related Files
- `SqlTestDataGenerator.Core/Services/EngineService.cs`
- `SqlTestDataGenerator.Core/Services/EnhancedDependencyResolver.cs`
- `SqlTestDataGenerator.Tests/CompleteWorkflowAutomatedTest.cs`
- `docs/foreign_key_constraint_fix.md`

## Impact
- ✅ **Resolved**: Oracle foreign key constraint violations
- ✅ **Improved**: Oracle-specific transaction handling
- ✅ **Enhanced**: Error reporting for Oracle constraint issues
- ✅ **Maintained**: Compatibility with other database types 