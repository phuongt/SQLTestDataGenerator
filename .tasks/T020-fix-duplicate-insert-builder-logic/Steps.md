# Task T020: Fix Duplicate INSERT Builder Logic

## Mục tiêu
Loại bỏ duplicate code giữa DataGenService và CoordinatedDataGenerator để fix generated column bug và đảm bảo consistency.

## Root Cause
- `DataGenService.GenerateBogusData()` có filter generated columns đúng
- `CoordinatedDataGenerator.BuildInsertStatement()` không filter → Bug
- **Code duplication** gây inconsistent updates

## Checklist các bước thực hiện

### 1. ✅ Phân tích và tạo common service
- [x] Tạo task folder T020
- [x] Tạo checklist Steps.md  
- [x] Tạo CommonInsertBuilder service
- [x] Define common interface cho INSERT building logic

### 2. ✅ Refactor existing code
- [x] Extract common logic từ DataGenService
- [x] Extract common logic từ CoordinatedDataGenerator
- [x] Update both services để dùng CommonInsertBuilder
- [ ] ❌ Issue: DataGenService vẫn chưa hoàn toàn dùng CommonInsertBuilder

### 3. 🧪 Test và validate
- [x] Build solution sau refactor
- [x] Run TC001 để verify fix hoạt động
- [ ] ❌ Test vẫn fail - cần debug deeper
- [ ] Validate INSERT statements không còn generated columns

### 4. 📝 Documentation và cleanup
- [ ] Update common defects với solution
- [ ] Clean up old duplicate code
- [ ] Add unit tests cho CommonInsertBuilder
- [ ] Confirm fix completion

## Expected Solution

### CommonInsertBuilder Service
```csharp
public class CommonInsertBuilder
{
    public string BuildInsertStatement(
        string tableName,
        Dictionary<string, object> record, 
        TableSchema tableSchema,
        DatabaseType databaseType)
    {
        // Filter out generated AND identity columns
        var generatedColumns = tableSchema.Columns
            .Where(c => c.IsGenerated || c.IsIdentity)
            .Select(c => c.ColumnName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
            
        var filteredRecord = record.Where(kvp => !generatedColumns.Contains(kvp.Key));
        
        var columns = string.Join(", ", filteredRecord.Select(kvp => QuoteIdentifier(kvp.Key, databaseType)));
        var values = string.Join(", ", filteredRecord.Select(kvp => FormatValue(kvp.Value)));
        
        return $"INSERT INTO {QuoteIdentifier(tableName, databaseType)} ({columns}) VALUES ({values})";
    }
}
```

## Expected Outcome
- ✅ Consistent INSERT building logic
- ✅ Generated columns được filter ở cả 2 paths
- ✅ TC001 pass successfully  
- ✅ No more duplicate code 