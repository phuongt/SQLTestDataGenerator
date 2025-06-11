# CRITICAL: Genericity Violation - Task T021

## ❌ **Vi phạm nghiêm trọng**
Tôi đã hardcode specific column patterns và values:

```csharp
// ❌ WRONG - Hardcode column name pattern
if (columnName.Contains("gender") || columnName.Contains("sex"))
    return faker.PickRandom("M", "F");
```

## ✅ **Nguyên tắc đúng**
Hệ thống phải **100% GENERIC** để work với:
- ✅ BẤT KỲ database nào
- ✅ BẤT KỲ table schema nào  
- ✅ BẤT KỲ SQL query nào
- ✅ BẤT KỲ column names nào

## 🔧 **Fix Strategy**
1. **Remove ALL hardcode column patterns**
2. **Use database metadata** (MaxLength, DataType, Constraints)
3. **Generate data based on SQL constraints**, not column names
4. **Add workspace rule** để prevent future violations

## 📝 **Lesson Learned**
Không bao giờ hardcode specific:
- Column names
- Table names  
- Specific values
- Business logic patterns

**Genericity là core principle!** 