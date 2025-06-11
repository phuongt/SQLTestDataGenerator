-- =====================================================
-- Script tạo tables: Users, Companies, Roles 
-- Database: MySQL (freedb_DBTest)
-- =====================================================

-- Disable foreign key checks để có thể drop tables
SET FOREIGN_KEY_CHECKS = 0;

-- Xóa các table cũ nếu tồn tại (theo thứ tự dependency)
DROP TABLE IF EXISTS user_roles;
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS roles;
DROP TABLE IF EXISTS companies;

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- 1. COMPANIES Table
-- =====================================================
CREATE TABLE companies (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    code VARCHAR(50) UNIQUE NOT NULL,
    address TEXT,
    phone VARCHAR(20),
    email VARCHAR(255),
    website VARCHAR(255),
    industry VARCHAR(100),
    employee_count INT DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- =====================================================
-- 2. ROLES Table
-- =====================================================
CREATE TABLE roles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    code VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    permissions JSON,
    level INT DEFAULT 1,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- =====================================================
-- 3. USERS Table
-- =====================================================
CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    full_name VARCHAR(255) GENERATED ALWAYS AS (CONCAT(first_name, ' ', last_name)) STORED,
    phone VARCHAR(20),
    address TEXT,
    date_of_birth DATE,
    gender ENUM('Male', 'Female', 'Other'),
    avatar_url VARCHAR(500),
    company_id INT,
    primary_role_id INT,
    salary DECIMAL(15,2),
    hire_date DATE,
    department VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    last_login_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    -- Foreign Keys
    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE SET NULL,
    FOREIGN KEY (primary_role_id) REFERENCES roles(id) ON DELETE SET NULL,
    
    -- Indexes
    INDEX idx_users_email (email),
    INDEX idx_users_username (username),
    INDEX idx_users_company (company_id),
    INDEX idx_users_role (primary_role_id),
    INDEX idx_users_active (is_active)
);

-- =====================================================
-- 4. USER_ROLES Table (Many-to-Many relationship)
-- =====================================================
CREATE TABLE user_roles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    role_id INT NOT NULL,
    assigned_by INT,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NULL,
    is_active BOOLEAN DEFAULT TRUE,
    
    -- Foreign Keys
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    FOREIGN KEY (assigned_by) REFERENCES users(id) ON DELETE SET NULL,
    
    -- Unique constraint
    UNIQUE KEY unique_user_role (user_id, role_id),
    
    -- Indexes
    INDEX idx_user_roles_user (user_id),
    INDEX idx_user_roles_role (role_id),
    INDEX idx_user_roles_active (is_active)
);

-- =====================================================
-- 5. INSERT SAMPLE DATA
-- =====================================================

-- Insert Companies
INSERT INTO companies (name, code, address, phone, email, website, industry, employee_count) VALUES
('Tech Solutions Inc', 'TSI001', '123 Tech Street, Silicon Valley, CA', '+1-555-0101', 'info@techsolutions.com', 'www.techsolutions.com', 'Technology', 150),
('Global Consulting Ltd', 'GCL002', '456 Business Ave, New York, NY', '+1-555-0202', 'contact@globalconsulting.com', 'www.globalconsulting.com', 'Consulting', 75),
('Innovation Labs', 'IL003', '789 Innovation Blvd, Austin, TX', '+1-555-0303', 'hello@innovationlabs.com', 'www.innovationlabs.com', 'Research', 200),
('Digital Marketing Pro', 'DMP004', '321 Marketing Way, Miami, FL', '+1-555-0404', 'info@digitalmarketingpro.com', 'www.digitalmarketingpro.com', 'Marketing', 50);

-- Insert Roles
INSERT INTO roles (name, code, description, level) VALUES
('Super Admin', 'SUPER_ADMIN', 'Full system access and control', 10),
('Company Admin', 'COMPANY_ADMIN', 'Full access within company scope', 8),
('Manager', 'MANAGER', 'Team management and reporting access', 6),
('Team Lead', 'TEAM_LEAD', 'Lead team members and projects', 5),
('Senior Developer', 'SENIOR_DEV', 'Senior development role with mentoring responsibilities', 4),
('Developer', 'DEVELOPER', 'Software development and coding', 3),
('Junior Developer', 'JUNIOR_DEV', 'Entry-level development role', 2),
('Intern', 'INTERN', 'Temporary learning position', 1),
('HR Manager', 'HR_MANAGER', 'Human resources management', 6),
('Sales Manager', 'SALES_MANAGER', 'Sales team management', 6),
('Marketing Specialist', 'MARKETING_SPEC', 'Marketing campaigns and analysis', 3),
('Customer Support', 'CUSTOMER_SUPPORT', 'Customer service and support', 2);

-- Insert Users
INSERT INTO users (username, email, password_hash, first_name, last_name, phone, date_of_birth, gender, company_id, primary_role_id, salary, hire_date, department) VALUES
('john.doe', 'john.doe@techsolutions.com', '$2y$10$example.hash.password.123', 'John', 'Doe', '+1-555-1001', '1985-03-15', 'Male', 1, 2, 120000.00, '2020-01-15', 'Engineering'),
('jane.smith', 'jane.smith@techsolutions.com', '$2y$10$example.hash.password.124', 'Jane', 'Smith', '+1-555-1002', '1988-07-22', 'Female', 1, 3, 95000.00, '2019-08-10', 'Engineering'),
('mike.johnson', 'mike.johnson@globalconsulting.com', '$2y$10$example.hash.password.125', 'Mike', 'Johnson', '+1-555-1003', '1990-11-08', 'Male', 2, 4, 85000.00, '2021-03-20', 'Consulting'),
('sarah.wilson', 'sarah.wilson@innovationlabs.com', '$2y$10$example.hash.password.126', 'Sarah', 'Wilson', '+1-555-1004', '1992-05-12', 'Female', 3, 5, 110000.00, '2020-09-15', 'Research'),
('david.brown', 'david.brown@digitalmarketingpro.com', '$2y$10$example.hash.password.127', 'David', 'Brown', '+1-555-1005', '1987-12-03', 'Male', 4, 11, 70000.00, '2022-01-10', 'Marketing'),
('lisa.garcia', 'lisa.garcia@techsolutions.com', '$2y$10$example.hash.password.128', 'Lisa', 'Garcia', '+1-555-1006', '1993-09-18', 'Female', 1, 6, 75000.00, '2021-06-01', 'Engineering'),
('robert.lee', 'robert.lee@globalconsulting.com', '$2y$10$example.hash.password.129', 'Robert', 'Lee', '+1-555-1007', '1989-04-25', 'Male', 2, 9, 90000.00, '2020-11-30', 'HR'),
('emily.davis', 'emily.davis@innovationlabs.com', '$2y$10$example.hash.password.130', 'Emily', 'Davis', '+1-555-1008', '1991-08-14', 'Female', 3, 7, 55000.00, '2023-02-15', 'Research');

-- Insert User-Role relationships (additional roles for users)
INSERT INTO user_roles (user_id, role_id, assigned_by) VALUES
(1, 1, 1),  -- John Doe also has Super Admin
(1, 5, 1),  -- John Doe also has Senior Developer
(2, 5, 1),  -- Jane Smith also has Senior Developer
(3, 6, 1),  -- Mike Johnson also has Developer
(4, 5, 1),  -- Sarah Wilson also has Senior Developer
(5, 10, 1), -- David Brown also has Sales Manager
(6, 6, 1),  -- Lisa Garcia also has Developer
(7, 9, 1),  -- Robert Lee confirmed HR Manager
(8, 7, 1);  -- Emily Davis also has Junior Developer

-- =====================================================
-- 6. CREATE USEFUL VIEWS
-- =====================================================

-- View: User details with company and role information
CREATE VIEW user_details AS
SELECT 
    u.id,
    u.username,
    u.email,
    u.full_name,
    u.phone,
    u.department,
    u.salary,
    u.hire_date,
    u.is_active as user_active,
    c.name as company_name,
    c.code as company_code,
    r.name as primary_role_name,
    r.code as primary_role_code,
    u.created_at,
    u.last_login_at
FROM users u
LEFT JOIN companies c ON u.company_id = c.id
LEFT JOIN roles r ON u.primary_role_id = r.id;

-- View: User roles summary
CREATE VIEW user_roles_summary AS
SELECT 
    u.id as user_id,
    u.username,
    u.full_name,
    GROUP_CONCAT(r.name ORDER BY r.level DESC SEPARATOR ', ') as all_roles,
    COUNT(ur.role_id) as total_roles
FROM users u
LEFT JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = TRUE
LEFT JOIN roles r ON ur.role_id = r.id
GROUP BY u.id, u.username, u.full_name;

-- =====================================================
-- 7. SAMPLE QUERIES FOR TESTING
-- =====================================================

-- Test query 1: Get all users with their company and primary role
-- SELECT * FROM user_details WHERE user_active = TRUE;

-- Test query 2: Get users by company
-- SELECT u.*, c.name as company_name FROM users u JOIN companies c ON u.company_id = c.id WHERE c.code = 'TSI001';

-- Test query 3: Get all roles for a specific user
-- SELECT u.full_name, r.name as role_name, ur.assigned_at FROM users u JOIN user_roles ur ON u.id = ur.user_id JOIN roles r ON ur.role_id = r.id WHERE u.username = 'john.doe';

-- Test query 4: Count users by company
-- SELECT c.name, COUNT(u.id) as user_count FROM companies c LEFT JOIN users u ON c.id = u.company_id GROUP BY c.id, c.name;

-- =====================================================
-- SCRIPT COMPLETED SUCCESSFULLY!
-- ===================================================== 