# 🚨 BUG ANALYSIS: Record Count Mismatch in Complex Query

## 📊 **PROBLEM STATEMENT**

**User Expectation**: Yêu cầu 15 records → Phải trả về đúng 15 rows
**Actual Result**: Yêu cầu 15 records → Engine chỉ trả về 5 rows

## 🔍 **ROOT CAUSE ANALYSIS**

### ✅ **Engine ĐÃ CÓ logic để generate targeted data:**

1. **SqlQueryParser**: ✅ Parse WHERE conditions
   - `u.first_name LIKE '%Phương%'` → Extracted: "Phương"
   - `YEAR(u.date_of_birth) = 1989` → Extracted: "1989"
   - `c.NAME LIKE '%VNEXT%'` → Extracted: "VNEXT"
   - `r.code LIKE '%DD%'` → Extracted: "DD"

2. **DataGenService**: ✅ Generate specific values
   - `GenerateLikeValue()` → "Phương", "VNEXT", "DD"
   - `GenerateYearEqualsDate()` → 1989 dates

### ❌ **BUG: Records KHÔNG match ALL conditions simultaneously!**

**Critical Issue**: Engine generate data theo từng column/table riêng lẻ, không ensure tất cả WHERE conditions được satisfied cùng lúc trong cùng 1 record set!

```csharp
// ❌ CURRENT LOGIC (WRONG):
// Table users: Some records có "Phương", some có birth_year=1989
// Table companies: Some có "VNEXT"
// Table roles: Some có "DD"
// → JOIN results: Very few matches (only 5 rows)

// ✅ CORRECT LOGIC (NEEDED):
// Ensure coordinated generation:
// - Users with "Phương" MUST also have birth_year=1989
// - Companies MUST have "VNEXT" 
// - Roles MUST have "DD"
// - user_roles MUST link these correctly
// → JOIN results: Guaranteed 15+ matches
```

## 🎯 **SPECIFIC BUG LOCATIONS**

### **File: `DataGenService.cs`**

#### **Bug 1: Random Data Generation**
```csharp:256-285
private string GenerateBogusValue(...)
{
    // ❌ PROBLEM: Generate theo DataType, không theo SQL requirements coordination
    return column.DataType.ToLower() switch
    {
        "varchar" => GenerateStringValue(...), // Random values
        "datetime" => GenerateDateValue(...),  // Random dates
        ...
    };
}
```

#### **Bug 2: Uncoordinated Column Generation**
```csharp:351-393
private string GenerateStringValue(...)
{
    // ✅ HAS logic for individual requirements
    if (columnRequirement != null)
    {
        value = GenerateValueForRequirement(...); // ✅ GOOD
    }
    
    // ❌ PROBLEM: No coordination between columns!
    // User có "Phương" không guarantee có birth_year=1989
}
```

#### **Bug 3: Missing Cross-Table Coordination**
```csharp:78-150
foreach (var table in tables)
{
    // ❌ PROBLEM: Generate each table independently
    // No ensure users/companies/roles will JOIN correctly
    for (int i = 1; i <= actualRecordCount; i++)
    {
        // Generate data without cross-table coordination
    }
}
```

## 💡 **SOLUTION APPROACH**

### **Strategy 1: Coordinated Generation**
```csharp
// ✅ NEW APPROACH:
// 1. Identify target records that MUST satisfy ALL WHERE conditions
// 2. Generate coordinated data for target records first
// 3. Fill remaining with regular data

// Target: Generate 15 records that satisfy ALL conditions:
// - Users: first_name="Phương" AND birth_year=1989  
// - Companies: name LIKE "%VNEXT%"
// - Roles: code LIKE "%DD%"
// - user_roles: Link them correctly with proper expires_at
```

### **Strategy 2: WHERE Condition Satisfaction Guarantee**
```csharp
// For complex queries with multiple WHERE conditions:
// 1. Parse ALL WHERE conditions as requirements
// 2. Generate target records that satisfy ALL conditions 
// 3. Ensure FK relationships link target records correctly
// 4. Validate final query will return expected count
```

## 🔧 **REQUIRED FIXES**

### **Fix 1: Add Coordinated Data Generation**
- New method: `GenerateCoordinatedDataForQuery()`
- Identify target record sets that satisfy ALL WHERE conditions
- Generate these first before regular random data

### **Fix 2: Cross-Table Coordination**
- Track which records satisfy which conditions
- Ensure FK relationships link target records
- Validate JOIN will produce expected results

### **Fix 3: Query Result Validation**
- After generation, run query and check result count
- If insufficient matches, generate more targeted data
- Retry until desired count is achieved

## 📈 **EXPECTED OUTCOME AFTER FIX**

- **Input**: DesiredRecordCount = 15
- **Engine**: Generate coordinated data ensuring 15+ records satisfy ALL WHERE conditions
- **Result**: Query returns exactly 15+ rows (not 5!)
- **Status**: ✅ **CORRECT**

## 🚀 **PRIORITY: CRITICAL**

This bug affects the core value proposition of the tool. Users expect the engine to generate data that satisfies their query requirements, not random data that accidentally matches few conditions. 