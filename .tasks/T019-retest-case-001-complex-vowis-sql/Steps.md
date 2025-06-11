# Task T019: Retest Case 001 - Complex Vowis SQL

## Mục tiêu
Test lại TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithRealMySQL để kiểm tra tính năng generate test data cho complex SQL query với nhiều JOIN tables.

## Checklist các bước thực hiện

### 1. ✅ Chuẩn bị môi trường test
- [x] Tạo task folder T019
- [x] Tạo checklist Steps.md
- [x] Kiểm tra database connection string
- [x] Kiểm tra test method TC001

### 2. ✅ Thực hiện test case 001
- [x] Chạy test TC001_15_ExecuteQueryWithTestDataAsync_ComplexVowisSQL_WithRealMySQL
- [x] Thu thập log và kết quả chi tiết
- [x] Phân tích success/failure case
- [x] Ghi nhận Generated Records vs Expected Records

### 3. ✅ Phân tích kết quả  
- [x] Kiểm tra SQL query execution
- [x] Validate INSERT statements được generate
- [x] Phân tích performance metrics
- [x] So sánh với expected behavior

### 4. ✅ Ghi nhận kết quả
- [x] Tạo test report chi tiết
- [x] Update common defects nếu có lỗi lặp lại
- [x] Đưa ra khuyến nghị next steps
- [x] Confirm test completion

## Expected Behavior
- **WITH MySQL connection**: Should generate exactly 15 records và execute query successfully  
- **WITHOUT MySQL connection**: Should fail với connection error message rõ ràng
- **Complex SQL**: Query có nhiều JOIN (users, companies, roles, user_roles) với WHERE conditions phức tạp

## Test Target
```sql
-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
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
  AND (u.is_active = 0 OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY))
ORDER BY ur.expires_at ASC, u.created_at DESC
``` 