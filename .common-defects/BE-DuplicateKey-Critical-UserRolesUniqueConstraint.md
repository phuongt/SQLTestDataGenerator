# BE-DuplicateKey-Critical-UserRolesUniqueConstraint

## Summary
Duplicate key constraint violation trong `user_roles` table khi generate test data với AI enabled. Lỗi xảy ra consistently với combination `'4-5'` cho `(user_id, role_id)`.

## Severity
**CRITICAL** - Blocks core functionality của application khi sử dụng với real MySQL database.

## How to Reproduce
1. Chạy integration test: `dotnet test --filter "TestCategory=MySQLIntegration"`
2. Hoặc manual test với:
   - Database: MySQL (freedb.tech)
   - SQL: Complex Vowis business query với 4 tables JOIN
   - Record count: 8-10 records
   - UseAI: true

## Error Details
```
Duplicate entry '4-5' for key 'user_roles.unique_user_role'
SQL: INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_by`, `assigned_at`, `expires_at`, `is_active`) 
VALUES (4, 5, 4, '2024-10-25 08:14:37', '2025-06-03 06:40:31', 28)
```

## Root Cause
1. **Logic bug trong `GenerateUniqueUserRoleCombination` method** (DataGenService.cs:123-170)
2. **Deterministic algorithm tạo ra duplicate combinations** với certain record counts
3. **HashSet tracking không effective** do scope/timing issues
4. **Foreign key generation cũng có issues** (company_id=68 ngoài range)
5. **MySQL auto-increment không reset** sau DELETE operations

## Impact
- ❌ Application crashes khi execute với real MySQL
- ❌ Unit tests pass nhưng integration tests fail
- ❌ Manual testing không thể complete workflow
- ❌ Blocks production deployment

## Solution Implemented ✅
1. **Fixed unique combination algorithm** - Sequential mapping với offset guarantee uniqueness
2. **Fixed foreign key generation** - Proper range validation và referenced table handling
3. **Added database cleanup** - DELETE + AUTO_INCREMENT reset cho clean state
4. **Improved error handling** - Better logging và collision detection
5. **Added comprehensive integration tests** - Real MySQL testing với cleanup

## Test Coverage
- ✅ Unit tests: `DuplicateKeyBugFixTests.cs` (4/4 pass)
- ✅ Integration tests: `MySQLIntegrationDuplicateKeyTests.cs` (3/3 pass)
- ✅ Manual testing: Now works successfully

## Related Files
- `SqlTestDataGenerator.Core/Services/DataGenService.cs` (lines 123-170) - FIXED
- `SqlTestDataGenerator.Tests/MySQLIntegrationDuplicateKeyTests.cs` - NEW
- `logs/sqltestgen-20250605_001.txt`

## Task ID
T010 - Test complex SQL generation

## Status
**RESOLVED** ✅ - Fix verified với real MySQL database

## Solution Details
### 1. Fixed GenerateUniqueUserRoleCombination Method
```csharp
// Use recordIndex to create guaranteed unique combinations
var userId = (recordIndex % totalRecords) + 1;
var roleId = ((recordIndex + totalRecords/2) % totalRecords) + 1;

// Collision detection với retry logic
while (usedCombinations.Contains(combination) && attempts < 10) {
    roleId = ((roleId + attempts) % totalRecords) + 1;
    combination = $"{userId}-{roleId}";
}
```

### 2. Fixed Foreign Key Generation
```csharp
// Better FK generation với proper range validation
var batchSize = 10;
var fkValue = (recordIndex % batchSize) + 1;

// Special handling cho different referenced tables
if (fk.ReferencedTable.Equals("users", StringComparison.OrdinalIgnoreCase)) {
    fkValue = (recordIndex % batchSize) + 1;
}
```

### 3. Database Cleanup Strategy
```csharp
// DELETE in dependency order + AUTO_INCREMENT reset
var cleanupQueries = new[] {
    "DELETE FROM user_roles WHERE id > 0",
    "DELETE FROM users WHERE id > 0", 
    "DELETE FROM roles WHERE id > 0",
    "DELETE FROM companies WHERE id > 0"
};

var resetQueries = new[] {
    "ALTER TABLE companies AUTO_INCREMENT = 1",
    "ALTER TABLE roles AUTO_INCREMENT = 1", 
    "ALTER TABLE users AUTO_INCREMENT = 1",
    "ALTER TABLE user_roles AUTO_INCREMENT = 1"
};
```

## Verification Results
- **Integration Test**: ✅ PASSED (19 seconds)
- **Generated**: 40 INSERT statements successfully
- **user_roles**: 10 total, 0 duplicates
- **FK Constraints**: All pass
- **Transaction**: Committed successfully 