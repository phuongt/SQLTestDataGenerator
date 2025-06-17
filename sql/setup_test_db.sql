-- Setup test database for SqlTestDataGenerator
-- SQLite database setup

-- Create users table
CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    email TEXT UNIQUE NOT NULL,
    age INTEGER NOT NULL,
    city TEXT,
    salary REAL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Create orders table for testing relationships
CREATE TABLE IF NOT EXISTS orders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER,
    product_name TEXT NOT NULL,
    amount REAL NOT NULL,
    order_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

-- Insert some sample data to start with
INSERT OR IGNORE INTO users (id, name, email, age, city, salary) VALUES 
(1, 'John Doe', 'john@example.com', 30, 'New York', 50000.0),
(2, 'Jane Smith', 'jane@example.com', 25, 'Los Angeles', 45000.0),
(3, 'Bob Johnson', 'bob@example.com', 35, 'Chicago', 60000.0);

INSERT OR IGNORE INTO orders (id, user_id, product_name, amount) VALUES
(1, 1, 'Laptop', 1200.00),
(2, 1, 'Mouse', 25.00),
(3, 2, 'Keyboard', 75.00),
(4, 3, 'Monitor', 300.00); 