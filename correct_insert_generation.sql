-- CORRECT INSERT GENERATION FOR TEST QUERIES
-- Để satisfy cả Simple SQL và Multi-table JOIN SQL

-- ===============================================
-- STEP 1: INSERT COMPANIES (Parent table first)
-- ===============================================
-- Need companies with name LIKE '%Tech%' for multi-table query
INSERT INTO companies (id, name, code, address, created_at, updated_at) VALUES
(1, 'VNEXTTech Solutions', 'VNEXT001', '123 Tech Street', NOW(), NOW()),
(2, 'TechCorp Innovation', 'TECH002', '456 Innovation Ave', NOW(), NOW()),
(3, 'Advanced Tech Systems', 'ATECH003', '789 Systems Blvd', NOW(), NOW()),
(4, 'Global Tech Hub', 'GTECH004', '321 Global Plaza', NOW(), NOW()),
(5, 'NextGen Technology', 'NGEN005', '654 NextGen Center', NOW(), NOW());

-- ===============================================
-- STEP 2: INSERT ROLES (Parent table)
-- ===============================================
-- Need roles with level >= 2 for multi-table query  
-- Need roles with code LIKE '%DD%' for complex query
INSERT INTO roles (id, name, code, level, created_at, updated_at) VALUES
(1, 'Developer Lead', 'DD_LEAD', 3, NOW(), NOW()),
(2, 'Senior Developer', 'DD_SENIOR', 2, NOW(), NOW()),
(3, 'DevOps Engineer', 'DD_DEVOPS', 2, NOW(), NOW()),
(4, 'Technical Director', 'DD_DIRECTOR', 4, NOW(), NOW()),
(5, 'Data Engineer', 'DD_DATA', 2, NOW(), NOW());

-- ===============================================
-- STEP 3: INSERT USERS (Child table)
-- ===============================================
-- Need users with:
-- - is_active = 1 (for both queries)
-- - name LIKE '%Phương%' AND birth year = 1989 (for complex query)
-- - Various companies for diversity
INSERT INTO users (id, username, email, password_hash, first_name, last_name, phone, address, date_of_birth, gender, avatar_url, company_id, primary_role_id, salary, hire_date, department, is_active, last_login_at, created_at, updated_at) VALUES
-- Users for Complex Query (tên Phương, sinh 1989)
(1, 'phuong_dev89', 'phuong.nguyen@vnexttech.com', 'hash1', 'Phương', 'Nguyễn', '0123456789', '123 Dev Street', '1989-05-15', 'Female', 'https://avatar1.com', 1, 1, 80000000, '2020-01-15', 'Development', 1, NOW(), '2020-01-15 08:00:00', NOW()),
(2, 'le_phuong89', 'phuong.le@techcorp.com', 'hash2', 'Lê', 'Phương', '0987654321', '456 Tech Ave', '1989-12-20', 'Male', 'https://avatar2.com', 2, 2, 75000000, '2019-06-10', 'Engineering', 1, NOW(), '2019-06-10 09:00:00', NOW()),
(3, 'phuong_senior', 'phuong.tran@advtech.com', 'hash3', 'Phương', 'Trần', '0555123456', '789 Systems Rd', '1989-03-08', 'Female', 'https://avatar3.com', 3, 4, 120000000, '2018-03-01', 'R&D', 1, NOW(), '2018-03-01 07:30:00', NOW()),

-- Additional active users for simple query results
(4, 'john_doe', 'john@vnexttech.com', 'hash4', 'John', 'Doe', '0111222333', '111 Main St', '1990-01-01', 'Male', 'https://avatar4.com', 1, 2, 60000000, '2021-01-01', 'Development', 1, NOW(), '2021-01-01 10:00:00', NOW()),
(5, 'jane_smith', 'jane@techcorp.com', 'hash5', 'Jane', 'Smith', '0444555666', '222 Oak Ave', '1985-06-15', 'Female', 'https://avatar5.com', 2, 3, 70000000, '2020-05-15', 'DevOps', 1, NOW(), '2020-05-15 11:00:00', NOW()),
(6, 'alice_wilson', 'alice@globaltech.com', 'hash6', 'Alice', 'Wilson', '0777888999', '333 Pine St', '1992-11-30', 'Female', 'https://avatar6.com', 4, 1, 65000000, '2022-02-01', 'Development', 1, NOW(), '2022-02-01 12:00:00', NOW()),
(7, 'bob_johnson', 'bob@nextgen.com', 'hash7', 'Bob', 'Johnson', '0123987654', '444 Elm St', '1988-04-12', 'Male', 'https://avatar7.com', 5, 5, 85000000, '2019-11-20', 'Data', 1, NOW(), '2019-11-20 13:00:00', NOW()),

-- Inactive users (is_active = 0) for filtering test
(8, 'inactive_user', 'inactive@test.com', 'hash8', 'Inactive', 'User', '0000000000', '000 Nowhere', '1980-01-01', 'Other', 'https://avatar8.com', 1, 1, 50000000, '2015-01-01', 'Old Dept', 0, NULL, '2015-01-01 14:00:00', NOW());

-- ===============================================
-- STEP 4: INSERT USER_ROLES (Junction table)
-- ===============================================
-- Need user_roles with:
-- - is_active = 1 (for multi-table query)
-- - expires_at conditions for complex query
-- - Role level >= 2 connections
INSERT INTO user_roles (id, user_id, role_id, assigned_at, expires_at, is_active, created_at, updated_at) VALUES
-- Active roles for Phương users (complex query targets)
(1, 1, 1, '2020-02-01 08:00:00', '2025-08-01 08:00:00', 1, NOW(), NOW()), -- Phương Nguyễn - DD_LEAD (level 3)
(2, 2, 2, '2019-07-01 09:00:00', '2025-12-31 23:59:59', 1, NOW(), NOW()), -- Lê Phương - DD_SENIOR (level 2)  
(3, 3, 4, '2018-04-01 07:30:00', '2025-06-30 23:59:59', 1, NOW(), NOW()), -- Phương Trần - DD_DIRECTOR (level 4)

-- Additional active roles for other users
(4, 4, 2, '2021-02-01 10:00:00', NULL, 1, NOW(), NOW()), -- John - DD_SENIOR
(5, 5, 3, '2020-06-01 11:00:00', NULL, 1, NOW(), NOW()), -- Jane - DD_DEVOPS  
(6, 6, 1, '2022-03-01 12:00:00', NULL, 1, NOW(), NOW()), -- Alice - DD_LEAD
(7, 7, 5, '2019-12-01 13:00:00', NULL, 1, NOW(), NOW()), -- Bob - DD_DATA

-- Expiring soon roles (for complex query CASE conditions)
(8, 1, 2, '2023-01-01 08:00:00', '2025-07-15 23:59:59', 1, NOW(), NOW()), -- Phương Nguyễn - expiring soon
(9, 2, 3, '2022-01-01 09:00:00', '2025-06-30 23:59:59', 1, NOW(), NOW()), -- Lê Phương - expiring soon

-- Inactive role assignments
(10, 8, 1, '2015-02-01 14:00:00', '2020-01-01 00:00:00', 0, NOW(), NOW()); -- Inactive user role

-- ===============================================
-- VERIFICATION QUERIES
-- ===============================================

-- Simple Query Test (should return active users, ordered by created_at DESC, limit 10)
-- SELECT * FROM users WHERE is_active = 1 ORDER BY created_at DESC LIMIT 10;
-- Expected: 7 users (IDs 7,6,4,3,2,1,5 in desc order by created_at)

-- Multi-table JOIN Test (should return users from Tech companies with level >= 2 roles)
-- Expected: Phương users + others from Tech companies with qualifying roles

-- Complex Query Test (should find Phương users, born 1989, from VNEXT companies, with DD roles, expiring soon)
-- Expected: Users 1,2,3 with their expiring role assignments 