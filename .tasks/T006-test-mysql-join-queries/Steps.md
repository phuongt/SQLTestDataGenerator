# T006 - Test MySQL Join Queries

## Mục tiêu
Test và kiểm tra khả năng xử lý query JOIN từ 2-3 table với điều kiện WHERE trên MySQL database.

## Checklist Steps

### 1. Setup và Chuẩn bị 
- [ ] Kiểm tra connection MySQL có sẵn không
- [ ] Tạo/Verify test database với multiple tables có relationships
- [ ] Chuẩn bị sample data cho các tables

### 2. Tạo Test Schema
- [ ] Tạo 3 tables: Users, Orders, Products với foreign keys
- [ ] Insert sample data vào các tables
- [ ] Verify relationships giữa các tables

### 3. Tạo Test Cases
- [ ] Test case 1: INNER JOIN 2 tables (Users + Orders) với WHERE
- [ ] Test case 2: LEFT JOIN 2 tables với complex WHERE conditions  
- [ ] Test case 3: INNER JOIN 3 tables (Users + Orders + Products)
- [ ] Test case 4: Mixed JOIN types với multiple WHERE clauses

### 4. Implement Test Code
- [ ] Tạo test class cho MySQL JOIN queries
- [ ] Implement test methods cho mỗi test case
- [ ] Add proper error handling và assertions

### 5. Execute và Validate
- [ ] Chạy tất cả test cases
- [ ] Verify kết quả query đúng expected data
- [ ] Check performance với larger datasets
- [ ] Log results và identify any issues

### 6. Documentation
- [ ] Document test results
- [ ] Record any defects found
- [ ] Update README với test instructions

## Expected Deliverables
- Functional MySQL test database với related tables
- Complete test suite cho JOIN queries
- Test results documentation
- Any defect reports nếu có issues

## Notes
- Sử dụng knowledge từ previous MySQL defects để tránh common issues
- Focus vào proper SQL syntax và identifier quoting cho MySQL
- Test với real data để validate type conversions 