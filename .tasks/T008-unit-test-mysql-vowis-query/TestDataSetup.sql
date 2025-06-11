-- MySQL Test Data Setup for Vowis Query Testing
-- Test user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc

-- =============================================================================
-- 1. SETUP TABLES (Sử dụng lại schema đã có)
-- =============================================================================

-- Drop existing test data first
DELETE FROM user_roles WHERE user_id >= 1000;
DELETE FROM users WHERE id >= 1000;
DELETE FROM companies WHERE id >= 1000;
DELETE FROM roles WHERE id >= 1000;

-- =============================================================================
-- 2. INSERT TEST COMPANIES
-- =============================================================================

INSERT INTO companies (id, name, code, address, phone, email, industry, employee_count, is_active) VALUES
-- Positive test cases - VNEXT companies
(1001, 'VNEXT Technology Solutions', 'VNEXT_TECH', '123 Tech Street, Ho Chi Minh City', '+84-28-1234-5678', 'info@vnext-tech.com', 'Technology', 500, TRUE),
(1002, 'VNEXT Digital Solutions', 'VNEXT_DIGITAL', '456 Digital Ave, Hanoi', '+84-24-8765-4321', 'contact@vnext-digital.com', 'Digital Services', 300, TRUE),
(1003, 'VNEXT Software Development', 'VNEXT_DEV', '789 Dev Boulevard, Da Nang', '+84-236-555-0123', 'dev@vnext-software.com', 'Software', 200, TRUE),

-- Negative test cases - Non-VNEXT companies  
(1004, 'FPT Software', 'FPT_SOFT', '100 FPT Street, Ho Chi Minh City', '+84-28-9999-8888', 'info@fpt.com', 'Technology', 1000, TRUE),
(1005, 'TMA Solutions', 'TMA_SOL', '200 TMA Road, Ho Chi Minh City', '+84-28-7777-6666', 'contact@tma.com', 'Software', 800, TRUE);

-- =============================================================================
-- 3. INSERT TEST ROLES  
-- =============================================================================

INSERT INTO roles (id, name, code, description, level, is_active) VALUES
-- Positive test cases - DD roles
(1001, 'Digital Director', 'DD', 'Giám đốc Digital transformation', 9, TRUE),
(1002, 'Data Director', 'DD_DATA', 'Giám đốc Data Analytics', 9, TRUE), 
(1003, 'Development Director', 'DD_DEV', 'Giám đốc Phát triển', 9, TRUE),

-- Negative test cases - Non-DD roles
(1004, 'Senior Manager', 'MANAGER', 'Quản lý cấp cao', 7, TRUE),
(1005, 'Team Lead', 'TEAM_LEAD', 'Trưởng nhóm', 5, TRUE),
(1006, 'Developer', 'DEVELOPER', 'Lập trình viên', 3, TRUE);

-- =============================================================================
-- 4. INSERT TEST USERS
-- =============================================================================

INSERT INTO users (id, username, email, password_hash, first_name, last_name, phone, date_of_birth, gender, company_id, primary_role_id, salary, department, hire_date, is_active, created_at) VALUES

-- =============================================================================
-- POSITIVE TEST CASES (Should return results)
-- =============================================================================

-- Test Case 1: Phương + 1989 + VNEXT + DD + Sắp nghỉ việc (is_active = 1, expires soon)
(1001, 'phuong.nguyen1989', 'phuong.nguyen@vnext-tech.com', 'hash123', 'Phương', 'Nguyễn', '+84-123-456-789', '1989-03-15', 'Female', 1001, 1001, 180000.00, 'Digital Transformation', '2018-01-15', TRUE, '2018-01-15 09:00:00'),

-- Test Case 2: Tên cuối là Phương + 1989 + VNEXT + DD + Đã nghỉ việc (is_active = 0)
(1002, 'minh.phuong1989', 'minh.phuong@vnext-digital.com', 'hash456', 'Minh', 'Phương', '+84-987-654-321', '1989-07-22', 'Male', 1002, 1002, 175000.00, 'Data Analytics', '2019-03-10', FALSE, '2019-03-10 10:00:00'),

-- Test Case 3: Phương + 1989 + VNEXT + DD + Sắp nghỉ việc (expires soon)  
(1003, 'phuong.tran1989', 'phuong.tran@vnext-dev.com', 'hash789', 'Phương', 'Trần', '+84-555-123-456', '1989-11-08', 'Female', 1003, 1003, 170000.00, 'Development', '2020-06-01', TRUE, '2020-06-01 11:00:00'),

-- =============================================================================
-- NEGATIVE TEST CASES (Should NOT return results)
-- =============================================================================

-- Test Case 4: Phương + 1989 + VNEXT + DD nhưng còn làm việc lâu dài (no expiration)
(1004, 'phuong.le1989', 'phuong.le@vnext-tech.com', 'hash101', 'Phương', 'Lê', '+84-777-888-999', '1989-12-25', 'Female', 1001, 1001, 185000.00, 'Digital Strategy', '2021-01-20', TRUE, '2021-01-20 08:00:00'),

-- Test Case 5: User không phải Phương + 1989 + VNEXT + DD
(1005, 'duc.nguyen1989', 'duc.nguyen@vnext-tech.com', 'hash202', 'Đức', 'Nguyễn', '+84-111-222-333', '1989-04-10', 'Male', 1001, 1001, 165000.00, 'Digital Marketing', '2019-08-15', TRUE, '2019-08-15 14:00:00'),

-- Test Case 6: Phương + KHÔNG sinh 1989 + VNEXT + DD  
(1006, 'phuong.hoang1990', 'phuong.hoang@vnext-digital.com', 'hash303', 'Phương', 'Hoàng', '+84-444-555-666', '1990-05-20', 'Female', 1002, 1002, 160000.00, 'Data Science', '2020-12-01', TRUE, '2020-12-01 16:00:00'),

-- Test Case 7: Phương + 1989 + KHÔNG VNEXT + DD
(1007, 'phuong.pham1989', 'phuong.pham@fpt.com', 'hash404', 'Phương', 'Phạm', '+84-666-777-888', '1989-09-14', 'Female', 1004, 1001, 150000.00, 'Digital Innovation', '2018-05-10', TRUE, '2018-05-10 12:00:00'),

-- Test Case 8: Phương + 1989 + VNEXT + KHÔNG DD
(1008, 'phuong.do1989', 'phuong.do@vnext-dev.com', 'hash505', 'Phương', 'Đỗ', '+84-999-000-111', '1989-08-30', 'Female', 1003, 1004, 155000.00, 'Project Management', '2019-11-20', TRUE, '2019-11-20 13:00:00');

-- =============================================================================
-- 5. INSERT USER_ROLES WITH EXPIRATION LOGIC
-- =============================================================================

INSERT INTO user_roles (user_id, role_id, assigned_by, assigned_at, expires_at, is_active) VALUES

-- =============================================================================  
-- POSITIVE CASES - Should match query conditions
-- =============================================================================

-- User 1001: Phương Nguyễn - DD role expires in 20 days (within 60 day threshold)
(1001, 1001, 1, '2018-01-15 09:00:00', DATE_ADD(NOW(), INTERVAL 20 DAY), TRUE),

-- User 1002: Minh Phương - DD role expired (user already inactive) 
(1002, 1002, 1, '2019-03-10 10:00:00', DATE_ADD(NOW(), INTERVAL -10 DAY), FALSE),

-- User 1003: Phương Trần - DD role expires in 45 days (within 60 day threshold)
(1003, 1003, 1, '2020-06-01 11:00:00', DATE_ADD(NOW(), INTERVAL 45 DAY), TRUE),

-- =============================================================================
-- NEGATIVE CASES - Should NOT match query conditions  
-- =============================================================================

-- User 1004: Phương Lê - DD role has no expiration (will work long term)
(1004, 1001, 1, '2021-01-20 08:00:00', NULL, TRUE),

-- User 1005: Đức Nguyễn - DD role expires in 90 days (beyond 60 day threshold) 
(1005, 1001, 1, '2019-08-15 14:00:00', DATE_ADD(NOW(), INTERVAL 90 DAY), TRUE),

-- User 1006: Phương Hoàng - DD role expires in 100 days (beyond 60 day threshold)
(1006, 1002, 1, '2020-12-01 16:00:00', DATE_ADD(NOW(), INTERVAL 100 DAY), TRUE),

-- User 1007: Phương Phạm - DD role no expiration but wrong company
(1007, 1001, 1, '2018-05-10 12:00:00', NULL, TRUE),

-- User 1008: Phương Đỗ - Wrong role (MANAGER instead of DD)
(1008, 1004, 1, '2019-11-20 13:00:00', DATE_ADD(NOW(), INTERVAL 30 DAY), TRUE);

-- =============================================================================
-- 6. VERIFICATION QUERIES
-- =============================================================================

-- Count test users by category
SELECT 'Total Test Users' as Category, COUNT(*) as Count FROM users WHERE id >= 1000
UNION ALL
SELECT 'Test Companies', COUNT(*) FROM companies WHERE id >= 1000  
UNION ALL
SELECT 'Test Roles', COUNT(*) FROM roles WHERE id >= 1000
UNION ALL
SELECT 'Test User Roles', COUNT(*) FROM user_roles WHERE user_id >= 1000;

-- Preview test data
SELECT u.id, u.first_name, u.last_name, u.date_of_birth, 
       c.name as company_name, r.name as role_name, 
       ur.expires_at, u.is_active
FROM users u
JOIN companies c ON u.company_id = c.id
JOIN user_roles ur ON u.id = ur.user_id  
JOIN roles r ON ur.role_id = r.id
WHERE u.id >= 1000
ORDER BY u.id; 