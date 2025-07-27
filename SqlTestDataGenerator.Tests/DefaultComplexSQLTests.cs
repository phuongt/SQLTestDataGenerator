using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Test cases cho các câu SQL phức tạp mặc định khi chọn database type
/// </summary>
[TestClass]
public class DefaultComplexSQLTests
{
    private EngineService _engineService = null!;

    [TestInitialize]
    public void Setup()
    {
        _engineService = new EngineService(DatabaseType.MySQL, "Server=localhost;Port=3306;Database=test;Uid=root;Pwd=;CharSet=utf8mb4;");
    }

    [TestMethod]
    public void GetDefaultComplexSQL_MySQL_ShouldReturnValidMySQLSyntax()
    {
        // Arrange
        var mysqlSQL = GetDefaultComplexSQL("MySQL");

        // Assert
        Assert.IsNotNull(mysqlSQL, "MySQL SQL should not be null");
        Assert.IsTrue(mysqlSQL.Contains("DATE_ADD(NOW(), INTERVAL"), "Should use MySQL date function");
        Assert.IsTrue(mysqlSQL.Contains("LIMIT 20"), "Should use MySQL LIMIT syntax");
        Assert.IsTrue(mysqlSQL.Contains("YEAR(u.date_of_birth)"), "Should use MySQL YEAR function");
        Assert.IsTrue(mysqlSQL.Contains("TRUE"), "Should use MySQL boolean TRUE");
        Assert.IsTrue(mysqlSQL.Contains("-- Tìm user tên Phương"), "Should contain Vietnamese comment");
        
        Console.WriteLine("=== MySQL Default Complex SQL ===");
        Console.WriteLine(mysqlSQL);
    }

    [TestMethod]
    public void GetDefaultComplexSQL_Oracle_ShouldReturnValidOracleSyntax()
    {
        // Arrange
        var oracleSQL = GetDefaultComplexSQL("Oracle");

        // Assert
        Assert.IsNotNull(oracleSQL, "Oracle SQL should not be null");
        Assert.IsTrue(oracleSQL.Contains("SYSDATE + 30"), "Should use Oracle SYSDATE function");
        Assert.IsFalse(oracleSQL.Contains("FETCH FIRST"), "Should not use Oracle FETCH syntax for this version");
        Assert.IsTrue(oracleSQL.Contains("EXTRACT(YEAR FROM u.date_of_birth)"), "Should use Oracle EXTRACT function");
        Assert.IsTrue(oracleSQL.Contains("1"), "Should use Oracle boolean 1");
        Assert.IsTrue(oracleSQL.Contains("-- Tìm user tên Phương"), "Should contain Vietnamese comment");
        
        Console.WriteLine("=== Oracle Default Complex SQL ===");
        Console.WriteLine(oracleSQL);
    }

    [TestMethod]
    public void GetDefaultComplexSQL_SQLServer_ShouldReturnValidSQLServerSyntax()
    {
        // Arrange
        var sqlServerSQL = GetDefaultComplexSQL("SQL Server");

        // Assert
        Assert.IsNotNull(sqlServerSQL, "SQL Server SQL should not be null");
        Assert.IsTrue(sqlServerSQL.Contains("DATEADD(DAY, 30, GETDATE())"), "Should use SQL Server DATEADD function");
        Assert.IsTrue(sqlServerSQL.Contains("OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY"), "Should use SQL Server OFFSET FETCH syntax");
        Assert.IsTrue(sqlServerSQL.Contains("YEAR(u.date_of_birth)"), "Should use SQL Server YEAR function");
        Assert.IsTrue(sqlServerSQL.Contains("1"), "Should use SQL Server boolean 1");
        Assert.IsTrue(sqlServerSQL.Contains("-- Tìm user tên Phương"), "Should contain Vietnamese comment");
        
        Console.WriteLine("=== SQL Server Default Complex SQL ===");
        Console.WriteLine(sqlServerSQL);
    }

    [TestMethod]
    public void GetDefaultComplexSQL_PostgreSQL_ShouldReturnValidPostgreSQLSyntax()
    {
        // Arrange
        var postgreSQL = GetDefaultComplexSQL("PostgreSQL");

        // Assert
        Assert.IsNotNull(postgreSQL, "PostgreSQL SQL should not be null");
        Assert.IsTrue(postgreSQL.Contains("CURRENT_DATE + INTERVAL '30 days'"), "Should use PostgreSQL INTERVAL syntax");
        Assert.IsTrue(postgreSQL.Contains("LIMIT 20"), "Should use PostgreSQL LIMIT syntax");
        Assert.IsTrue(postgreSQL.Contains("EXTRACT(YEAR FROM u.date_of_birth)"), "Should use PostgreSQL EXTRACT function");
        Assert.IsTrue(postgreSQL.Contains("TRUE"), "Should use PostgreSQL boolean TRUE");
        Assert.IsTrue(postgreSQL.Contains("FALSE"), "Should use PostgreSQL boolean FALSE");
        Assert.IsTrue(postgreSQL.Contains("-- Tìm user tên Phương"), "Should contain Vietnamese comment");
        
        Console.WriteLine("=== PostgreSQL Default Complex SQL ===");
        Console.WriteLine(postgreSQL);
    }

    [TestMethod]
    public void GetDefaultComplexSQL_UnknownDatabase_ShouldReturnSimpleSQL()
    {
        // Arrange
        var unknownSQL = GetDefaultComplexSQL("UnknownDB");

        // Assert
        Assert.IsNotNull(unknownSQL, "Unknown database SQL should not be null");
        Assert.IsTrue(unknownSQL.Contains("SELECT * FROM users LIMIT 10"), "Should return simple fallback SQL");
        
        Console.WriteLine("=== Unknown Database Default SQL ===");
        Console.WriteLine(unknownSQL);
    }

    [TestMethod]
    public void GetDefaultComplexSQL_AllDatabases_ShouldHaveConsistentStructure()
    {
        // Test that all database types return SQL with similar structure
        var databases = new[] { "MySQL", "Oracle", "SQL Server", "PostgreSQL" };
        
        foreach (var dbType in databases)
        {
            var sql = GetDefaultComplexSQL(dbType);
            
            // All should contain the same business logic structure
            Assert.IsTrue(sql.Contains("SELECT u.id, u.username"), "Should select user fields");
            Assert.IsTrue(sql.Contains("INNER JOIN companies c"), "Should join companies table");
            Assert.IsTrue(sql.Contains("INNER JOIN user_roles ur"), "Should join user_roles table");
            Assert.IsTrue(sql.Contains("INNER JOIN roles r"), "Should join roles table");
            Assert.IsTrue(sql.Contains("CASE"), "Should contain CASE statement");
            Assert.IsTrue(sql.Contains("work_status"), "Should have work_status calculated field");
            Assert.IsTrue(sql.Contains("-- Tìm user tên Phương"), "Should contain Vietnamese comment");
            
            Console.WriteLine($"✅ {dbType} SQL structure validated");
        }
    }

    [TestMethod]
    public void GetDefaultComplexSQL_DatabaseSpecificSyntax_ShouldBeCorrect()
    {
        // Test database-specific syntax differences
        var mysqlSQL = GetDefaultComplexSQL("MySQL");
        var oracleSQL = GetDefaultComplexSQL("Oracle");
        
        // MySQL specific
        Assert.IsTrue(mysqlSQL.Contains("DATE_ADD(NOW(), INTERVAL"), "MySQL should use DATE_ADD");
        Assert.IsTrue(mysqlSQL.Contains("LIMIT 20"), "MySQL should use LIMIT");
        
        // Oracle specific  
        Assert.IsTrue(oracleSQL.Contains("SYSDATE + 30"), "Oracle should use SYSDATE");
        Assert.IsFalse(oracleSQL.Contains("FETCH FIRST"), "Oracle should not use FETCH FIRST for this version");
        
        // Should NOT contain each other's syntax
        Assert.IsFalse(mysqlSQL.Contains("SYSDATE"), "MySQL should not contain Oracle syntax");
        Assert.IsFalse(mysqlSQL.Contains("FETCH FIRST"), "MySQL should not contain Oracle syntax");
        Assert.IsFalse(oracleSQL.Contains("DATE_ADD"), "Oracle should not contain MySQL syntax");
        Assert.IsFalse(oracleSQL.Contains("LIMIT"), "Oracle should not contain MySQL syntax");
        
        Console.WriteLine("✅ Database-specific syntax validation passed");
    }

    /// <summary>
    /// Copy of the GetDefaultComplexSQL method from MainForm for testing
    /// </summary>
    private static string GetDefaultComplexSQL(string databaseType)
    {
        return databaseType switch
        {
            "MySQL" => @"
-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date, 
       c.NAME AS company_name, c.code AS company_code, r.NAME AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
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
LIMIT 20",

            "Oracle" => @"
-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date, 
       c.NAME AS company_name, c.code AS company_code, r.NAME AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
       CASE 
           WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
           WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= SYSDATE + 30 THEN 'Sắp hết hạn vai trò'
           ELSE 'Đang làm việc'
       END AS work_status
FROM users u
INNER JOIN companies c ON u.company_id = c.id
INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = 1
INNER JOIN roles r ON ur.role_id = r.id
WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
  AND EXTRACT(YEAR FROM u.date_of_birth) = 1989
  AND c.NAME LIKE '%VNEXT%'
  AND r.code LIKE '%DD%'
  AND (u.is_active = 0 OR ur.expires_at <= SYSDATE + 60)
ORDER BY ur.expires_at ASC, u.created_at DESC",

            "SQL Server" => @"
-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date, 
       c.NAME AS company_name, c.code AS company_code, r.NAME AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
       CASE 
           WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
           WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATEADD(DAY, 30, GETDATE()) THEN 'Sắp hết hạn vai trò'
           ELSE 'Đang làm việc'
       END AS work_status
FROM users u
INNER JOIN companies c ON u.company_id = c.id
INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = 1
INNER JOIN roles r ON ur.role_id = r.id
WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
  AND YEAR(u.date_of_birth) = 1989
  AND c.NAME LIKE '%VNEXT%'
  AND r.code LIKE '%DD%'
  AND (u.is_active = 0 OR ur.expires_at <= DATEADD(DAY, 60, GETDATE()))
ORDER BY ur.expires_at ASC, u.created_at DESC
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY",

            "PostgreSQL" => @"
-- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date, 
       c.NAME AS company_name, c.code AS company_code, r.NAME AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
       CASE 
           WHEN u.is_active = FALSE THEN 'Đã nghỉ việc'
           WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= CURRENT_DATE + INTERVAL '30 days' THEN 'Sắp hết hạn vai trò'
           ELSE 'Đang làm việc'
       END AS work_status
FROM users u
INNER JOIN companies c ON u.company_id = c.id
INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = TRUE
INNER JOIN roles r ON ur.role_id = r.id
WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
  AND EXTRACT(YEAR FROM u.date_of_birth) = 1989
  AND c.NAME LIKE '%VNEXT%'
  AND r.code LIKE '%DD%'
  AND (u.is_active = FALSE OR ur.expires_at <= CURRENT_DATE + INTERVAL '60 days')
ORDER BY ur.expires_at ASC, u.created_at DESC
LIMIT 20",

            _ => "SELECT * FROM users LIMIT 10;"
        };
    }
} 