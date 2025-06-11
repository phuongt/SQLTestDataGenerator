-- Kiểm tra data thực tế trong MySQL sau khi SqlQueryParserV3 generate
-- Connection: Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012

-- 1. Kiểm tra companies có VNEXT
SELECT COUNT(*) as company_count, 
       GROUP_CONCAT(DISTINCT name LIMIT 5) as sample_names
FROM companies 
WHERE name LIKE '%VNEXT%';

-- 2. Kiểm tra roles có DD code  
SELECT COUNT(*) as role_count,
       GROUP_CONCAT(DISTINCT code LIMIT 5) as sample_codes
FROM roles 
WHERE code LIKE '%DD%';

-- 3. Kiểm tra users năm sinh 1989
SELECT COUNT(*) as user_count_1989,
       COUNT(CASE WHEN first_name LIKE '%Ph%' THEN 1 END) as users_with_ph
FROM users 
WHERE YEAR(date_of_birth) = 1989;

-- 4. Kiểm tra encoding của first_name
SELECT DISTINCT first_name, HEX(first_name) as hex_encoding
FROM users 
WHERE first_name LIKE '%Ph%' 
LIMIT 5;

-- 5. Test complex WHERE conditions với data hiện tại
SELECT COUNT(*) as matching_records
FROM users u
INNER JOIN companies c ON u.company_id = c.id
INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = TRUE
INNER JOIN roles r ON ur.role_id = r.id
WHERE (u.first_name LIKE '%Ph%' OR u.last_name LIKE '%Ph%')  -- Thử với Ph thay vì Phương
  AND YEAR(u.date_of_birth) = 1989
  AND c.name LIKE '%VNEXT%'
  AND r.code LIKE '%DD%'
  AND (u.is_active = 0 OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY));

-- 6. Relaxed test - chỉ kiểm tra một vài conditions
SELECT COUNT(*) as relaxed_count
FROM users u
INNER JOIN companies c ON u.company_id = c.id
WHERE YEAR(u.date_of_birth) = 1989
  AND c.name LIKE '%VNEXT%';

-- DEBUG: Check actual generated company data from TC001 test
-- Purpose: Xác định tại sao query trả về 0 rows khi expect 15 rows

-- 1. Check all generated companies và tìm VNEXT pattern
SELECT 'COMPANIES' as table_name, COUNT(*) as total_records FROM companies;

SELECT 'COMPANIES WITH VNEXT' as pattern_check, 
       name, code, industry
FROM companies 
WHERE name LIKE '%VNEXT%' 
ORDER BY id DESC 
LIMIT 10;

-- 2. Check all companies (không filter VNEXT) để see actual patterns
SELECT 'ALL COMPANIES' as debug_info,
       id, name, code, industry
FROM companies 
ORDER BY id DESC 
LIMIT 10;

-- 3. Check generated users với Phương pattern
SELECT 'USERS WITH PHƯƠNG' as user_check,
       id, first_name, last_name, date_of_birth, company_id
FROM users 
WHERE (first_name LIKE '%Phương%' OR last_name LIKE '%Phương%')
  AND YEAR(date_of_birth) = 1989
ORDER BY id DESC
LIMIT 10;

-- 4. Check generated roles với DD pattern  
SELECT 'ROLES WITH DD' as role_check,
       id, name, code, level
FROM roles
WHERE code LIKE '%DD%'
ORDER BY id DESC
LIMIT 10;

-- 5. Check user_roles với correct conditions
SELECT 'USER_ROLES ACTIVE' as ur_check,
       id, user_id, role_id, expires_at, is_active
FROM user_roles
WHERE is_active = 1  -- Query cần ur.is_active = TRUE
ORDER BY id DESC
LIMIT 10;

-- 6. EXACT QUERY từ TC001 để debug tại sao 0 rows
SELECT 'TC001 EXACT QUERY DEBUG' as final_debug,
       u.id, u.first_name, u.last_name, u.date_of_birth,
       c.NAME AS company_name, c.code AS company_code,
       r.NAME AS role_name, r.code AS role_code,
       ur.expires_at, u.is_active as user_active, ur.is_active as role_active
FROM users u
INNER JOIN companies c ON u.company_id = c.id
INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = TRUE  -- ❗ This is the issue
INNER JOIN roles r ON ur.role_id = r.id
WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
  AND YEAR(u.date_of_birth) = 1989
  AND c.NAME LIKE '%VNEXT%'  -- ❗ This is also an issue
  AND r.code LIKE '%DD%'
  AND (u.is_active = 0 OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY))
ORDER BY ur.expires_at ASC, u.created_at DESC; 