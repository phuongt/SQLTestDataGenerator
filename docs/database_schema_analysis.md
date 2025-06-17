# Database Schema Analysis for MySQL Test

## Tables and Dependencies (từ FK constraint errors):

### 1. companies (Parent table)
```sql
CREATE TABLE companies (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(255),
    code VARCHAR(100),
    address TEXT,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);
```

### 2. roles (Parent table)
```sql
CREATE TABLE roles (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(255),
    code VARCHAR(100),
    level INT,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);
```

### 3. users (Child table)
```sql
CREATE TABLE users (
    id INT PRIMARY KEY AUTO_INCREMENT,
    username VARCHAR(255),
    email VARCHAR(255),
    password_hash VARCHAR(255),
    first_name VARCHAR(255),
    last_name VARCHAR(255),
    phone VARCHAR(50),
    address TEXT,
    date_of_birth DATE,
    gender ENUM('Male', 'Female', 'Other'),
    avatar_url TEXT,
    company_id INT,
    primary_role_id INT,
    salary DECIMAL(10,2),
    hire_date DATE,
    department VARCHAR(255),
    is_active TINYINT(1) DEFAULT 1,
    last_login_at TIMESTAMP,
    created_at TIMESTAMP,
    updated_at TIMESTAMP,
    
    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE SET NULL,
    FOREIGN KEY (primary_role_id) REFERENCES roles(id) ON DELETE SET NULL
);
```

### 4. user_roles (Junction table)
```sql
CREATE TABLE user_roles (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT,
    role_id INT,
    assigned_at TIMESTAMP,
    expires_at TIMESTAMP,
    is_active TINYINT(1) DEFAULT 1,
    created_at TIMESTAMP,
    updated_at TIMESTAMP,
    
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
);
```

## Dependency Order:
1. companies (no dependencies)
2. roles (no dependencies)  
3. users (depends on companies, roles)
4. user_roles (depends on users, roles) 