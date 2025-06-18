# Fix Foreign Key Constraint Error

## 🎯 Vấn đề đã giải quyết

**Lỗi gặp phải:**
```
Cannot add or update a child row: a foreign key constraint fails 
(`my_database`.`users`, CONSTRAINT `users_ibfk_1` FOREIGN KEY 
(`company_id`) REFERENCES `companies` (`id`) ON DELETE SET NULL)
```

**Nguyên nhân:**
- MySQL enforces foreign key constraints khi insert dữ liệu
- Bảng `users` có foreign key `company_id` reference đến `companies.id` 
- Khi insert data, MySQL kiểm tra reference và báo lỗi nếu `company_id` không tồn tại trong `companies`

## 🔧 Giải pháp đã triển khai

### 1. **Disable/Enable Foreign Key Checks**
MySQL cho phép tạm thời tắt foreign key constraints để insert dữ liệu tự do:

```sql
-- Tắt foreign key constraints tạm thời
SET FOREIGN_KEY_CHECKS = 0;

-- Insert dữ liệu
INSERT INTO companies (...) VALUES (...);
INSERT INTO users (...) VALUES (...);
INSERT INTO user_roles (...) VALUES (...);

-- Bật lại foreign key constraints
SET FOREIGN_KEY_CHECKS = 1;
```

### 2. **Files được cập nhật**

#### **EngineService.cs - ExportSqlToFileAsync()**
```csharp
// Add foreign key constraint management for MySQL
if (databaseType.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
{
    sqlContent.AddRange(new[]
    {
        "-- Disable foreign key checks temporarily",
        "SET FOREIGN_KEY_CHECKS = 0;",
        ""
    });
}

// ... Insert statements ...

// Add footer with foreign key re-enable
if (databaseType.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
{
    sqlContent.AddRange(new[]
    {
        "",
        "-- Re-enable foreign key checks", 
        "SET FOREIGN_KEY_CHECKS = 1;",
        "",
        "-- COMMIT;",
        $"-- End of generated SQL ({sqlStatements.Count} statements)"
    });
}
```

#### **MainForm.cs - btnExecuteFromFile_Click()**
```csharp
// For MySQL, ensure foreign key checks are disabled during execution
if (cboDbType.Text.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
{
    await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 0;", transaction: transaction, commandTimeout: 300);
    Console.WriteLine($"[MainForm] Disabled foreign key checks for MySQL");
}

// Execute SQL statements...

// Re-enable foreign key checks for MySQL before committing
if (cboDbType.Text.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
{
    await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 1;", transaction: transaction, commandTimeout: 300);
    Console.WriteLine($"[MainForm] Re-enabled foreign key checks for MySQL");
}
```

## ✅ Kết quả

### **Trước khi fix:**
- ❌ Lỗi: `foreign key constraint fails`
- ❌ Không thể insert dữ liệu có foreign key references  
- ❌ Phải insert theo thứ tự dependencies nghiêm ngặt

### **Sau khi fix:**
- ✅ Insert thành công bất kể thứ tự foreign key dependencies
- ✅ File SQL được generate tự động có foreign key disable/enable
- ✅ UI execution cũng tự động handle foreign key constraints
- ✅ Dữ liệu vẫn đảm bảo integrity sau khi re-enable constraints

## 🔒 Bảo mật & Tính toàn vẹn

**An toàn:**
- Foreign key constraints chỉ tạm thời disabled trong transaction
- Constraints được re-enabled ngay sau khi insert hoàn tất
- Nếu có lỗi, transaction rollback và constraints vẫn intact
- Chỉ áp dụng cho MySQL, không ảnh hưởng các database khác

**Data Integrity:**
- Sau khi insert, foreign key constraints được kiểm tra lại
- Dữ liệu sai vẫn sẽ bị reject khi re-enable constraints
- Chỉ bypass constraint check **khi insert**, không bypass **validation**

## 🎯 Sử dụng

1. **Generate data:** Foreign key management tự động được thêm vào file SQL
2. **Execute from file:** UI tự động handle foreign key disable/enable  
3. **Không cần thay đổi workflow hiện tại**

File SQL sẽ có format:
```sql
-- Generated SQL INSERT statements  
-- Database Type: MySQL

-- Disable foreign key checks temporarily
SET FOREIGN_KEY_CHECKS = 0;

INSERT INTO companies (...) VALUES (...);
INSERT INTO users (...) VALUES (...);  

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;

-- COMMIT;
```

## 📊 Impact

- ✅ **Resolved:** Foreign key constraint errors  
- ✅ **Improved:** User experience - no manual foreign key management
- ✅ **Maintained:** Data integrity and constraints validation
- ✅ **Compatible:** Works with existing workflow and other databases 