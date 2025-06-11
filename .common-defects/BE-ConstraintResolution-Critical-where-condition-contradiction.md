# WHERE Condition Contradiction trong Constraint Resolution

## Summary
Engine generate data thành công nhưng không satisfy complex WHERE conditions, dẫn đến query trả về 0 rows thay vì expected count.

## How to Reproduce

### Test Case
```csharp
// TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithGeminiAI
var request = new QueryExecutionRequest
{
    SqlQuery = @"
        SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date, 
               c.NAME AS company_name, c.code AS company_code, r.NAME AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
               CASE 
                   WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                   WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn vai trò'
                   ELSE 'Đang làm việc'
               END AS work_status
        FROM users u
        INNER JOIN companies c ON u.company_id = c.id
        INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = TRUE
        INNER JOIN roles r ON ur.role_id = r.id
        WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
          AND YEAR(u.date_of_birth) = 1989
          AND c.NAME LIKE '%VNEXT%'
          AND r.code LIKE '%DD%'
          AND (u.is_active = 0 OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY))",
    DesiredRecordCount = 15,
    UseAI = false // Using Bogus generator
};
```

### Expected vs Actual
- **Expected**: 15 rows returned từ final query
- **Actual**: 0 rows returned (60 INSERT statements executed successfully)

## Root Cause

### 1. Boolean Contradiction
```
[QUERY-AWARE] users.is_active: Generated 0 for = 0 condition
[QUERY-AWARE] user_roles.is_active: Generated 0 for = 0 condition
```

**Contradiction**:
- JOIN condition: `ur.is_active = TRUE` 
- WHERE condition: `u.is_active = 0 OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY)`
- Engine generates `user_roles.is_active = 0` BUT JOIN requires `TRUE`

### 2. Pattern Matching Issues
- **LIKE conditions**: `'%Phương%'`, `'%VNEXT%'`, `'%DD%'`
- **Date functions**: `YEAR(u.date_of_birth) = 1989`
- **Dynamic dates**: `DATE_ADD(NOW(), INTERVAL 60 DAY)`

Bogus generator không hiểu được những patterns phức tạp này.

## Solution

### ⚡ Immediate Fix
```csharp
// In SqlQueryParser.cs - Enhance ParseEqualityConditions
private void ParseEqualityConditions(string whereClause, SqlDataRequirements requirements)
{
    // Existing logic...
    
    // 🎯 NEW: Check for contradictions with JOIN conditions
    foreach (var joinReq in requirements.JoinRequirements)
    {
        // If JOIN says ur.is_active = TRUE, don't allow WHERE to set it to FALSE
        if (/* contradiction detected */)
        {
            // Prioritize JOIN condition over WHERE condition
            // Or create reconciled value
        }
    }
}
```

### 🔄 Enhanced ParseJoinRequirements
```csharp
// In SqlQueryParser.cs - Extract additional conditions from JOIN ON clause
private void ExtractJoinRequirements(string cleanQuery, SqlDataRequirements requirements)
{
    // Existing JOIN parsing...
    
    // 🎯 GENERIC FIX: Parse additional WHERE conditions in JOIN ON clause
    // Extract conditions like "AND ur.is_active = TRUE"
    ParseEqualityConditions(onClause, requirements);
    ParseComparisonConditions(onClause, requirements);
    ParseNullChecks(onClause, requirements);
}
```

## Workaround
Use AI-enhanced generation (TC001_AI) for complex constraint scenarios:
```csharp
var request = new QueryExecutionRequest
{
    UseAI = true, // Enable AI generation
    OpenAiApiKey = geminiApiKey,
    // ... same complex SQL
};
// Result: ✅ 10/10 rows returned successfully
```

## Related Issues
- **File**: `SqlTestDataGenerator.Core/Services/SqlQueryParser.cs` line 138
- **Similar**: BE-RegexPattern-Critical-sqlqueryparser-not-extracting-where.md
- **Impact**: Complex business queries fail constraint satisfaction

## Prevention
1. **Unit tests** for constraint resolution logic
2. **Integration tests** với real complex WHERE conditions  
3. **AI fallback** mechanism for complex scenarios
4. **Constraint validation** trước khi execute INSERT statements 