# Task T037: Sửa lỗi SQL Syntax và UTF-8 Character Handling

## Mô tả
Sửa 2 lỗi quan trọng:
1. **SQL Syntax Compatibility**: DATE_ADD(NOW(), INTERVAL 30 DAY) parsing issues
2. **Vietnamese Character Handling**: CASE statement với tiếng Việt gây lỗi UTF-8

## Checklist Steps

### Step 1: Phân tích DATE_ADD parsing issues
- [ ] Kiểm tra các file SQL parser xử lý DATE_ADD
- [ ] Tìm pattern parsing cho MySQL DATE_ADD function
- [ ] Test các trường hợp DATE_ADD khác nhau

### Step 2: Cải thiện DATE_ADD pattern matching
- [ ] Cập nhật regex pattern trong SqlQueryParser.cs
- [ ] Cập nhật pattern trong ComprehensiveConstraintExtractor.cs
- [ ] Thêm fallback handling cho MySQL-specific syntax

### Step 3: Phân tích Vietnamese UTF-8 character issues
- [ ] Kiểm tra CASE statement với tiếng Việt trong codebase
- [ ] Tìm encoding issues trong string processing
- [ ] Test Vietnamese characters trong SQL generation

### Step 4: Sửa UTF-8 encoding issues
- [ ] Đảm bảo database connection sử dụng UTF-8
- [ ] Fix character encoding trong SQL statement generation
- [ ] Cập nhật connection string với charset=utf8mb4

### Step 5: Tạo unit tests cho fixes
- [ ] Test DATE_ADD parsing với various formats
- [ ] Test Vietnamese characters trong CASE statements
- [ ] Test full integration với MySQL

### Step 6: Validate fixes
- [ ] Run existing tests
- [ ] Test với VOWIS business query
- [ ] Capture results và verify fixes

## Kết quả mong muốn
- DATE_ADD syntax được parse chính xác
- Vietnamese characters trong CASE statements không bị lỗi
- UTF-8 encoding hoạt động đúng với MySQL 