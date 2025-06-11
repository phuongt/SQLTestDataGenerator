# Test Report: TC001 Complex Vowis SQL - Generated Column Issue

## Tóm tắt Test
- **Test Case**: TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithRealMySQL
- **Thời gian**: 2025-01-17
- **Status**: ❌ FAILED due to Generated Column Issue
- **Root Cause**: Engine INSERT generated column `full_name` với explicit value

## Kết quả Chi tiết

### ✅ Engine Functionality Confirmed
1. **MySQL Connection**: ✅ Connected successfully to `localhost:3306/my_database`
2. **Schema Analysis**: ✅ Detected 4 tables (users, companies, user_roles, roles)
3. **Generated Column Detection**: ✅ Detected `full_name` as STORED GENERATED column
4. **INSERT Generation**: ✅ Generated 60 INSERT statements
5. **Execution Progress**: ✅ Successfully executed 29/60 statements (companies + roles)

### ❌ Failure Point
**Table**: `users`
**Issue**: Generated column `full_name` được include trong INSERT statement
**Error**: `The value specified for generated column 'full_name' in table 'users' is not allowed.`

### Sample Problem Statement
```sql
INSERT INTO `users` (
    `username`, `email`, `password_hash`, `first_name`, `last_name`, 
    `full_name`,  -- ❌ This should NOT be included (GENERATED COLUMN)
    `phone`, `address`, `date_of_birth`, `gender`, `avatar_url`, 
    `company_id`, `primary_role_id`, `salary`, `hire_date`, `department`, 
    `is_active`, `last_login_at`, `created_at`, `updated_at`, `id`
) VALUES (
    'Maya Oberbrunner', 'Palma31@yahoo.com', 'aut_1', 
    'Phương_001_373', 'Phương_001_806', 
    'Benton Morissette',  -- ❌ This value should not be provided
    '397-880-1541', '0800 Bergnaum Roads', '1989-01-12 23:20:55', 
    'DefaultValue_1', 'ea_1', 1, 1, 131.167186822656037, 
    '2024-07-02 02:16:24', 'vero_1', '0', '2024-12-29 19:26:50', 
    '2025-01-17 03:40:45', '2024-10-05 06:46:29', 1
)
```

## Schema Analysis
Engine đã detect correctly:
```
[SqlMetadataService] Found generated column: full_name with EXTRA: STORED GENERATED, IS_GENERATED: 1 
[SqlMetadataService] SET IsGenerated=TRUE for column: full_name
```

## Root Cause Analysis
1. **Detection**: ✅ Engine correctly identified `full_name` as generated column
2. **Flag Setting**: ✅ `IsGenerated=TRUE` was set correctly  
3. **INSERT Generation**: ❌ Despite `IsGenerated=TRUE`, column was still included in INSERT
4. **Bug Location**: Likely in INSERT statement builder ignoring `IsGenerated` flag

## Impact Assessment
- **Severity**: 🔴 **CRITICAL** - Blocks all tables với generated columns
- **Scope**: Affects MySQL tables với STORED GENERATED hoặc DEFAULT_GENERATED columns
- **Workaround**: None currently - generated columns must be excluded from INSERT

## Performance Metrics
- **Total Time**: 0.67 seconds  
- **Successful INSERTs**: 29/60 (companies + roles)
- **Failed Table**: users (due to generated column)
- **Connection**: Stable throughout test

## Next Steps Required
1. **Fix INSERT Builder**: Exclude columns với `IsGenerated=TRUE` 
2. **Update Test Logic**: Generated columns should not be in INSERT statements
3. **Add Unit Tests**: Test generated column exclusion specifically
4. **Validate Fix**: Re-run TC001 after fix

## Conclusion
🎯 **ENGINE HOẠT ĐỘNG TỐT** - chỉ cần fix generated column handling trong INSERT builder.
Đây là một bug implementation cụ thể, không phải architectural issue. 