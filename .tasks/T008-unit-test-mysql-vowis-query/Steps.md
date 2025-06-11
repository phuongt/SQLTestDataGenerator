# Task T008: Unit Test MySQL Vowis Query

## Mô tả Task
Tạo unit test script cho MySQL query phức tạp tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc

## Query Target
```sql
-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date,
       c.NAME AS company_name, c.code AS company_code,
       r.NAME AS role_name, r.code AS role_code,
       ur.expires_at AS role_expires,
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

## Checklist Steps

### Step 1: Phân tích và Setup
- [ ] 1.1 Phân tích query requirements và test cases cần thiết
- [ ] 1.2 Kiểm tra schema database hiện tại
- [ ] 1.3 Xác định các table cần thiết: users, companies, user_roles, roles
- [ ] 1.4 Tạo project structure cho unit test

### Step 2: Tạo Test Data Setup
- [ ] 2.1 Tạo schema tables nếu chưa có
- [ ] 2.2 Tạo test data cho positive test cases (tìm thấy kết quả)
- [ ] 2.3 Tạo test data cho negative test cases (không tìm thấy)
- [ ] 2.4 Tạo edge cases data (boundary conditions)

### Step 3: Implement Unit Test Framework
- [ ] 3.1 Tạo test class cho MySQL query testing
- [ ] 3.2 Setup database connection và transaction rollback
- [ ] 3.3 Implement helper methods cho data assertion
- [ ] 3.4 Tạo SQL query execution wrapper

### Step 4: Viết Test Cases
- [ ] 4.1 Test Case 1: User Phương, 1989, VNEXT, DD, sắp nghỉ việc (should return results)
- [ ] 4.2 Test Case 2: User Phương, 1989, VNEXT, DD, đã nghỉ việc (should return results)
- [ ] 4.3 Test Case 3: User không phải Phương (should return empty)
- [ ] 4.4 Test Case 4: User Phương nhưng không sinh 1989 (should return empty)
- [ ] 4.5 Test Case 5: User Phương, 1989 nhưng không công ty VNEXT (should return empty)
- [ ] 4.6 Test Case 6: User Phương, 1989, VNEXT nhưng không vai trò DD (should return empty)
- [ ] 4.7 Test Case 7: User Phương, 1989, VNEXT, DD nhưng còn làm việc lâu dài (should return empty)

### Step 5: Test Business Logic
- [ ] 5.1 Test CASE WHEN logic cho work_status
- [ ] 5.2 Test date calculations (DATE_ADD, INTERVAL logic)
- [ ] 5.3 Test LIKE patterns cho name matching
- [ ] 5.4 Test JOIN conditions đúng
- [ ] 5.5 Test ORDER BY logic

### Step 6: Execute và Validate
- [ ] 6.1 Chạy tất cả unit tests
- [ ] 6.2 Verify test coverage đầy đủ
- [ ] 6.3 Kiểm tra performance của query
- [ ] 6.4 Validate kết quả với business requirements

### Step 7: Documentation và Cleanup
- [ ] 7.1 Document test results và findings
- [ ] 7.2 Log any issues vào .common-defects nếu có
- [ ] 7.3 Cleanup test data và close connections
- [ ] 7.4 Create summary report

## Lưu ý về Common Defects
- Chú ý MySQL backtick parsing issues (từ .common-defects)
- Kiểm tra JSON column generation nếu có
- Verify information schema type conversion
- Tránh duplicate project classes issues

## Expected Output
- Unit test project với full test coverage
- Test data setup scripts
- Detailed test report với pass/fail status
- Performance metrics cho query
- Documentation về business logic validation 