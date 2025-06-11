# DB-MySQL-Critical-regex-backtick-parsing

## Tóm tắt vấn đề
SqlMetadataService không thể extract table names từ MySQL queries sử dụng backticks (`) để quote table names. Regex patterns chỉ hỗ trợ brackets [] và standard identifiers.

## Cách tái tạo
1. Chạy query với MySQL backtick syntax:
```sql
SELECT u.name FROM `users` u
INNER JOIN `orders` o ON u.id = o.user_id
```
2. SqlMetadataService.ExtractTablesFromQuery() returns 0 tables
3. Error: "Không thể xác định bảng nào từ SQL query"

## Root Cause Analysis

### Current Regex Patterns trong SqlMetadataService.cs:
```csharp
// Line ~230: FROM pattern
@"\bFROM\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\])(?:\s+(?:AS\s+)?(\w+))?"

// Line ~240: JOIN pattern  
@"\b(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+|OUTER\s+)?JOIN\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\])(?:\s+(?:AS\s+)?(\w+))?"
```

**Problem:** Patterns chỉ match:
- Standard identifiers: `table_name`
- SQL Server brackets: `[table_name]`
- **KHÔNG MATCH MySQL backticks:** `` `table_name` ``

### Example Failed Query Analysis:
```
Input: FROM `users` u
Current Regex: \bFROM\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\])(?:\s+(?:AS\s+)?(\w+))?
Match Result: 0 matches (FAILS)

Expected: Should extract "users" từ `users`
```

## Solution/Workaround

### 1. Update Regex Patterns để support MySQL backticks:

```csharp
// OLD:
@"\bFROM\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\])(?:\s+(?:AS\s+)?(\w+))?"

// NEW (support backticks):  
@"\bFROM\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\]|`([^`]+)`)(?:\s+(?:AS\s+)?(\w+))?"
```

### 2. Update JOIN pattern tương tự:
```csharp
// OLD:
@"\b(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+|OUTER\s+)?JOIN\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\])(?:\s+(?:AS\s+)?(\w+))?"

// NEW:
@"\b(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+|OUTER\s+)?JOIN\s+(?:(?:\w+\.)?(\w+)|\[([^\]]+)\]|`([^`]+)`)(?:\s+(?:AS\s+)?(\w+))?"
```

### 3. Handle group extraction cho backtick matches trong code logic

## Impact Assessment
- **Severity:** CRITICAL - MySQL JOIN queries hoàn toàn không work
- **Affected:** Tất cả MySQL queries sử dụng backticks (best practice)
- **Components:** SqlMetadataService.ExtractTablesFromQuery()
- **Workaround:** Viết query không dùng backticks (không recommended cho MySQL)

## Related Task/Commit ID
- Task: T006-test-mysql-join-queries
- File: SqlTestDataGenerator.Core/Services/SqlMetadataService.cs
- Method: ExtractTablesFromQuery()
- Date: 2025-06-04

## Status
🔴 CRITICAL - Cần fix ngay để test MySQL JOIN queries

## Fix Priority
HIGH - Blocking MySQL functionality testing

## Test Cases to Validate Fix
1. `FROM \`users\` u` → should extract "users"
2. `JOIN \`orders\` o` → should extract "orders"  
3. `FROM \`order_items\`` → should extract "order_items"
4. Mixed: `FROM users u JOIN \`orders\` o` → should extract both 