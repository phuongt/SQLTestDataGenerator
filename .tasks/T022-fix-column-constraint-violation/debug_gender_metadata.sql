-- Debug gender column metadata từ MySQL
-- Để hiểu tại sao ENUM không được detect

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    COLUMN_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    EXTRA,
    GENERATION_EXPRESSION
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'my_database' 
  AND TABLE_NAME = 'users' 
  AND COLUMN_NAME = 'gender'; 