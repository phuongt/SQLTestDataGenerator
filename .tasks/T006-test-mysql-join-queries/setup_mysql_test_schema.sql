-- MySQL Test Schema for JOIN Queries
-- Tạo 3 tables: Users, Orders, Products với foreign key relationships

-- Drop tables if exist (reverse order due to foreign keys)
DROP TABLE IF EXISTS order_items;
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS products;
DROP TABLE IF EXISTS users;

-- Create Users table
CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(150) UNIQUE NOT NULL,
    age INT,
    city VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create Products table  
CREATE TABLE products (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    category VARCHAR(50),
    stock_quantity INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create Orders table
CREATE TABLE orders (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    total_amount DECIMAL(10,2) NOT NULL,
    status VARCHAR(20) DEFAULT 'pending',
    order_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    shipping_address VARCHAR(200),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Create Order Items table (for many-to-many relationship)
CREATE TABLE order_items (
    id INT AUTO_INCREMENT PRIMARY KEY,
    order_id INT NOT NULL,
    product_id INT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    unit_price DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE,
    FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE
);

-- Insert sample data
-- Users
INSERT INTO users (name, email, age, city) VALUES
('Nguyễn Văn An', 'an.nguyen@email.com', 28, 'Hà Nội'),
('Trần Thị Bình', 'binh.tran@email.com', 32, 'TP.HCM'),
('Lê Văn Cường', 'cuong.le@email.com', 25, 'Đà Nẵng'),
('Phạm Thị Dung', 'dung.pham@email.com', 29, 'Hà Nội'),
('Hoàng Văn Em', 'em.hoang@email.com', 35, 'Cần Thơ'),
('Vũ Thị Phương', 'phuong.vu@email.com', 27, 'TP.HCM');

-- Products
INSERT INTO products (name, price, category, stock_quantity) VALUES
('Laptop Dell XPS 13', 25000000.00, 'Electronics', 10),
('iPhone 15 Pro', 30000000.00, 'Electronics', 15),
('Samsung Galaxy S24', 22000000.00, 'Electronics', 20),
('Áo thun Nike', 500000.00, 'Clothing', 50),
('Quần jeans Levi\'s', 1200000.00, 'Clothing', 30),
('Giày Adidas Ultraboost', 3500000.00, 'Shoes', 25),
('Sách lập trình Python', 250000.00, 'Books', 100),
('Tai nghe Sony WH-1000XM5', 8000000.00, 'Electronics', 12);

-- Orders
INSERT INTO orders (user_id, total_amount, status, shipping_address) VALUES
(1, 25500000.00, 'completed', '123 Phố Huế, Hai Bà Trưng, Hà Nội'),
(2, 30500000.00, 'shipped', '456 Nguyễn Huệ, Quận 1, TP.HCM'),
(3, 22250000.00, 'pending', '789 Hàn Thuyên, Hải Châu, Đà Nẵng'),
(1, 1700000.00, 'completed', '123 Phố Huế, Hai Bà Trưng, Hà Nội'),
(4, 8500000.00, 'processing', '321 Kim Mã, Ba Đình, Hà Nội'),
(5, 3750000.00, 'shipped', '654 Trần Hưng Đạo, Ninh Kiều, Cần Thơ');

-- Order Items
INSERT INTO order_items (order_id, product_id, quantity, unit_price) VALUES
(1, 1, 1, 25000000.00),  -- An mua Laptop Dell
(1, 7, 2, 250000.00),    -- An mua 2 cuốn sách
(2, 2, 1, 30000000.00),  -- Bình mua iPhone
(2, 4, 1, 500000.00),    -- Bình mua áo thun
(3, 3, 1, 22000000.00),  -- Cường mua Samsung
(3, 7, 1, 250000.00),    -- Cường mua sách
(4, 5, 1, 1200000.00),   -- An mua quần jeans
(4, 4, 1, 500000.00),    -- An mua áo thun
(5, 8, 1, 8000000.00),   -- Dung mua tai nghe
(5, 4, 1, 500000.00),    -- Dung mua áo thun
(6, 6, 1, 3500000.00),   -- Em mua giày
(6, 7, 1, 250000.00);    -- Em mua sách

-- Show tables info
SHOW TABLES;

-- Show sample data
SELECT 'Users table:' as Info;
SELECT * FROM users LIMIT 3;

SELECT 'Products table:' as Info;
SELECT * FROM products LIMIT 3;

SELECT 'Orders table:' as Info;
SELECT * FROM orders LIMIT 3;

SELECT 'Order Items table:' as Info;
SELECT * FROM order_items LIMIT 5; 