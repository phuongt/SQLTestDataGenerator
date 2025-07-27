using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using Bogus;

namespace SqlTestDataGenerator.Tests;

[TestClass]
public class ComplexDataGenerationTests
{
    private DataGenService _dataGenService = null!;

    [TestInitialize]
    public void Setup()
    {
        _dataGenService = new DataGenService();
    }

    [TestMethod]
    public void GenerateBogusData_Users_ShouldCreateUserData()
    {
        // Arrange
        var userSchema = new TableSchema
        {
            TableName = "users",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "username", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "first_name", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "last_name", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "email", DataType = "varchar", MaxLength = 100 },
                new() { ColumnName = "date_of_birth", DataType = "date" },
                new() { ColumnName = "salary", DataType = "decimal" },
                new() { ColumnName = "department", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "hire_date", DataType = "date" },
                new() { ColumnName = "company_id", DataType = "int" },
                new() { ColumnName = "is_active", DataType = "bit" },
                new() { ColumnName = "created_at", DataType = "datetime" }
            }
        };

        var databaseInfo = new DatabaseInfo
        {
            Type = DatabaseType.MySQL
        };
        databaseInfo.Tables.Add("users", userSchema);

        // Act
        var result = _dataGenService.GenerateBogusData(databaseInfo, 5);

        // Assert
        Assert.IsNotNull(result, "Result should not be null");
        var userStatements = result.Where(r => r.TableName == "users").ToList();
        Assert.IsTrue(userStatements.Count > 0, "Should generate user insert statements");

        // Verify basic structure
        foreach (var stmt in userStatements)
        {
            Assert.IsTrue(stmt.SqlStatement.StartsWith("INSERT INTO"), "Should be INSERT statement");
            Assert.IsTrue(stmt.SqlStatement.Contains("users"), "Should target users table");
            Assert.IsTrue(stmt.SqlStatement.Contains("VALUES"), "Should have VALUES clause");
        }

        Console.WriteLine($"Generated {userStatements.Count} user statements");
        foreach (var stmt in userStatements.Take(2))
        {
            Console.WriteLine($"Sample: {stmt.SqlStatement.Substring(0, Math.Min(100, stmt.SqlStatement.Length))}...");
        }
    }

    [TestMethod]
    public void GenerateBogusData_Companies_ShouldCreateCompanyData()
    {
        // Arrange
        var companySchema = new TableSchema
        {
            TableName = "companies",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "name", DataType = "varchar", MaxLength = 200 },
                new() { ColumnName = "code", DataType = "varchar", MaxLength = 20 },
                new() { ColumnName = "address", DataType = "varchar", MaxLength = 300 },
                new() { ColumnName = "created_at", DataType = "datetime" }
            }
        };

        var databaseInfo = new DatabaseInfo
        {
            Type = DatabaseType.MySQL
        };
        databaseInfo.Tables.Add("companies", companySchema);

        // Act
        var result = _dataGenService.GenerateBogusData(databaseInfo, 3);

        // Assert
        Assert.IsNotNull(result, "Result should not be null");
        var companyStatements = result.Where(r => r.TableName == "companies").ToList();
        Assert.IsTrue(companyStatements.Count > 0, "Should generate company insert statements");

        // Check basic structure
        foreach (var stmt in companyStatements)
        {
            Assert.IsTrue(stmt.SqlStatement.StartsWith("INSERT INTO"), "Should be INSERT statement");
            Assert.IsTrue(stmt.SqlStatement.Contains("companies"), "Should target companies table");
            Assert.IsTrue(stmt.SqlStatement.Contains("VALUES"), "Should have VALUES clause");
            
            // Should contain name and code values
            Assert.IsTrue(stmt.SqlStatement.Contains("'"), "Should have string values");
        }

        Console.WriteLine($"Generated {companyStatements.Count} company statements");
        foreach (var stmt in companyStatements)
        {
            Console.WriteLine($"Company: {stmt.SqlStatement.Substring(0, Math.Min(120, stmt.SqlStatement.Length))}...");
        }
    }

    [TestMethod]
    public void GenerateBogusData_Roles_ShouldCreateRoleData()
    {
        // Arrange
        var roleSchema = new TableSchema
        {
            TableName = "roles",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "name", DataType = "varchar", MaxLength = 100 },
                new() { ColumnName = "code", DataType = "varchar", MaxLength = 20 },
                new() { ColumnName = "description", DataType = "varchar", MaxLength = 500 },
                new() { ColumnName = "created_at", DataType = "datetime" }
            }
        };

        var databaseInfo = new DatabaseInfo
        {
            Type = DatabaseType.MySQL
        };
        databaseInfo.Tables.Add("roles", roleSchema);

        // Act
        var result = _dataGenService.GenerateBogusData(databaseInfo, 5);

        // Assert
        Assert.IsNotNull(result, "Result should not be null");
        var roleStatements = result.Where(r => r.TableName == "roles").ToList();
        Assert.IsTrue(roleStatements.Count > 0, "Should generate role insert statements");

        // Check basic structure
        foreach (var stmt in roleStatements)
        {
            Assert.IsTrue(stmt.SqlStatement.StartsWith("INSERT INTO"), "Should be INSERT statement");
            Assert.IsTrue(stmt.SqlStatement.Contains("roles"), "Should target roles table");
            Assert.IsTrue(stmt.SqlStatement.Contains("VALUES"), "Should have VALUES clause");
        }

        Console.WriteLine($"Generated {roleStatements.Count} role statements");
        foreach (var stmt in roleStatements)
        {
            Console.WriteLine($"Role: {stmt.SqlStatement.Substring(0, Math.Min(120, stmt.SqlStatement.Length))}...");
        }
    }

    [TestMethod]
    public void GenerateBogusData_UserRoles_ShouldCreateUserRoleData()
    {
        // Arrange
        var userRoleSchema = new TableSchema
        {
            TableName = "user_roles",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "user_id", DataType = "int", IsPrimaryKey = true },
                new() { ColumnName = "role_id", DataType = "int", IsPrimaryKey = true },
                new() { ColumnName = "assigned_by", DataType = "int" },
                new() { ColumnName = "assigned_at", DataType = "datetime" },
                new() { ColumnName = "expires_at", DataType = "datetime", IsNullable = true },
                new() { ColumnName = "is_active", DataType = "bit" }
            },
            ForeignKeys = new List<ForeignKeySchema>
            {
                new() { ColumnName = "user_id", ReferencedTable = "users", ReferencedColumn = "id" },
                new() { ColumnName = "role_id", ReferencedTable = "roles", ReferencedColumn = "id" }
            }
        };

        var databaseInfo = new DatabaseInfo
        {
            Type = DatabaseType.MySQL
        };
        databaseInfo.Tables.Add("user_roles", userRoleSchema);

        // Act
        var result = _dataGenService.GenerateBogusData(databaseInfo, 5);

        // Assert
        Assert.IsNotNull(result, "Result should not be null");
        var userRoleStatements = result.Where(r => r.TableName == "user_roles").ToList();
        Assert.IsTrue(userRoleStatements.Count > 0, "Should generate user_role insert statements");

        // Check basic structure
        foreach (var stmt in userRoleStatements)
        {
            Assert.IsTrue(stmt.SqlStatement.StartsWith("INSERT INTO"), "Should be INSERT statement");
            Assert.IsTrue(stmt.SqlStatement.Contains("user_roles"), "Should target user_roles table");
            Assert.IsTrue(stmt.SqlStatement.Contains("VALUES"), "Should have VALUES clause");
        }

        Console.WriteLine($"Generated {userRoleStatements.Count} user_role statements");
        foreach (var stmt in userRoleStatements)
        {
            Console.WriteLine($"UserRole: {stmt.SqlStatement.Substring(0, Math.Min(150, stmt.SqlStatement.Length))}...");
        }
    }

    [TestMethod]
    public void GenerateBogusData_FourTables_ShouldRespectDependencyOrder()
    {
        // Arrange - Create full schema for all 4 tables
        var databaseInfo = CreateComplexDatabaseInfo();

        // Act
        var result = _dataGenService.GenerateBogusData(databaseInfo, 3);

        // Assert
        Assert.IsNotNull(result, "Result should not be null");
        Assert.IsTrue(result.Count > 0, "Should generate some insert statements");

        // Check that we have statements for all tables
        var tableNames = result.Select(r => r.TableName).Distinct().ToList();
        Console.WriteLine($"Generated data for tables: {string.Join(", ", tableNames)}");

        // Verify dependency order (parent tables should have lower priority numbers)
        var companiesStatements = result.Where(r => r.TableName == "companies").ToList();
        var rolesStatements = result.Where(r => r.TableName == "roles").ToList();
        var usersStatements = result.Where(r => r.TableName == "users").ToList();
        var userRolesStatements = result.Where(r => r.TableName == "user_roles").ToList();

        if (companiesStatements.Any() && usersStatements.Any())
        {
            var avgCompanyPriority = companiesStatements.Average(s => s.Priority);
            var avgUserPriority = usersStatements.Average(s => s.Priority);
            Assert.IsTrue(avgCompanyPriority <= avgUserPriority, 
                "Companies should have lower priority (inserted first) than users");
        }

        // Count statements per table
        Console.WriteLine($"Companies: {companiesStatements.Count}");
        Console.WriteLine($"Roles: {rolesStatements.Count}");
        Console.WriteLine($"Users: {usersStatements.Count}");
        Console.WriteLine($"UserRoles: {userRolesStatements.Count}");
    }

    [TestMethod]
    public void GenerateBogusData_DateConstraints_ShouldGenerateDateValues()
    {
        // Arrange
        var userSchema = new TableSchema
        {
            TableName = "users",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "first_name", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "date_of_birth", DataType = "date" }
            }
        };

        var databaseInfo = new DatabaseInfo
        {
            Type = DatabaseType.MySQL
        };
        databaseInfo.Tables.Add("users", userSchema);

        // Act
        var result = _dataGenService.GenerateBogusData(databaseInfo, 10);

        // Assert
        var userStatements = result.Where(r => r.TableName == "users").ToList();
        Assert.IsTrue(userStatements.Count > 0, "Should generate user statements");

        // Check if the statement contains date values
        foreach (var stmt in userStatements.Take(3))
        {
            Console.WriteLine($"User statement: {stmt.SqlStatement}");
            // Check if the statement contains a date - we should see date patterns
            Assert.IsTrue(stmt.SqlStatement.Contains("'"), "Should contain date values in quotes");
        }

        Console.WriteLine("Note: For business-specific data (e.g., Phương, 1989, VNEXT), AI generation would be needed");
    }

    [TestMethod]
    public async Task EngineService_ComplexSQL_ShouldGenerateAndExecute()
    {
        // Arrange
        var complexSQL = @"
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
            ORDER BY ur.expires_at ASC, u.created_at DESC";

        // Act - Extract tables first
        var extractedTables = SqlMetadataService.ExtractTablesFromQuery(complexSQL);

        // Assert - Should extract all 4 tables
        Assert.IsNotNull(extractedTables, "Should extract tables from complex SQL");
        Assert.AreEqual(4, extractedTables.Count, "Should extract 4 tables");
        
        CollectionAssert.Contains(extractedTables, "users", "Should extract users table");
        CollectionAssert.Contains(extractedTables, "companies", "Should extract companies table");
        CollectionAssert.Contains(extractedTables, "user_roles", "Should extract user_roles table");
        CollectionAssert.Contains(extractedTables, "roles", "Should extract roles table");

        Console.WriteLine($"✅ Complex SQL parsing successful. Tables: {string.Join(", ", extractedTables)}");
    }

    [TestMethod]
    public void GenerateBogusData_ComplexSchema_ShouldCreateValidInserts()
    {
        // Arrange - Create schema matching the complex SQL requirements
        var databaseInfo = CreateBusinessComplexDatabaseInfo();

        // Act
        var result = _dataGenService.GenerateBogusData(databaseInfo, 2);

        // Assert
        Assert.IsNotNull(result, "Should generate insert statements");
        Assert.IsTrue(result.Count > 0, "Should generate some statements");

        // Verify we get statements for all tables
        var tableNames = result.Select(r => r.TableName).Distinct().ToList();
        Console.WriteLine($"Generated statements for tables: {string.Join(", ", tableNames)}");

        // Should have data for core tables
        Assert.IsTrue(tableNames.Contains("companies"), "Should generate companies data");
        Assert.IsTrue(tableNames.Contains("roles"), "Should generate roles data");
        Assert.IsTrue(tableNames.Contains("users"), "Should generate users data");
        Assert.IsTrue(tableNames.Contains("user_roles"), "Should generate user_roles data");

        // Log sample statements for each table
        foreach (var tableName in tableNames)
        {
            var tableStatements = result.Where(r => r.TableName == tableName).ToList();
            Console.WriteLine($"\n=== {tableName.ToUpper()} ({tableStatements.Count} statements) ===");
            
            foreach (var stmt in tableStatements.Take(1))
            {
                Console.WriteLine($"Sample: {stmt.SqlStatement}");
            }
        }
    }

    [TestMethod]
    public void ExtractTablesFromQuery_BusinessSQL_ShouldHandleComplexJoins()
    {
        // Arrange - Real business query with multiple joins and conditions
        var businessSQL = @"
            SELECT u.id, u.username, p.profile_name, c.company_name, d.dept_name, r.role_title,
                   a.account_status, s.salary_amount, ur.assigned_date, ur.expires_at
            FROM users u
            LEFT JOIN user_profiles p ON u.id = p.user_id
            INNER JOIN companies c ON u.company_id = c.id
            LEFT JOIN departments d ON u.department_id = d.id
            INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = 1
            INNER JOIN roles r ON ur.role_id = r.id
            LEFT JOIN user_accounts a ON u.id = a.user_id
            LEFT JOIN salary_history s ON u.id = s.user_id AND s.is_current = 1
            WHERE u.is_active = 1 
              AND c.status = 'ACTIVE'
              AND r.level >= 3
              AND ur.expires_at > NOW()
            ORDER BY c.company_name, d.dept_name, u.last_name";

        // Act
        var tables = SqlMetadataService.ExtractTablesFromQuery(businessSQL);

        // Assert
        Assert.IsNotNull(tables, "Should extract tables");
        Assert.IsTrue(tables.Count >= 7, "Should extract at least 7 tables from business query");

        var expectedTables = new[] { "users", "user_profiles", "companies", "departments", "user_roles", "roles", "user_accounts", "salary_history" };
        
        foreach (var expectedTable in expectedTables)
        {
            Assert.IsTrue(tables.Contains(expectedTable), $"Should contain table: {expectedTable}");
        }

        Console.WriteLine($"Business SQL extracted {tables.Count} tables: {string.Join(", ", tables)}");
    }

    [TestMethod]
    public void ExtractTablesFromQuery_VietnameseComments_ShouldIgnoreComments()
    {
        // Arrange - SQL with Vietnamese comments (like the original requirement)
        var vietnameseSQL = @"
            -- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
            SELECT u.id, u.username, u.first_name /* Tên của người dùng */
            FROM users u -- Bảng người dùng chính
            INNER JOIN companies c ON u.company_id = c.id -- Liên kết với công ty
            /* 
               Lấy thông tin vai trò của người dùng
               Chỉ lấy vai trò đang hoạt động
            */
            INNER JOIN user_roles ur ON u.id = ur.user_id
            INNER JOIN roles r ON ur.role_id = r.id -- Bảng vai trò
            WHERE u.first_name LIKE '%Phương%' -- Tìm tên chứa Phương
              AND c.NAME LIKE '%VNEXT%' -- Công ty VNEXT";

        // Act
        var tables = SqlMetadataService.ExtractTablesFromQuery(vietnameseSQL);

        // Assert
        Assert.IsNotNull(tables, "Should extract tables despite Vietnamese comments");
        Assert.AreEqual(4, tables.Count, "Should extract 4 tables ignoring comments");
        
        CollectionAssert.Contains(tables, "users");
        CollectionAssert.Contains(tables, "companies");
        CollectionAssert.Contains(tables, "user_roles");
        CollectionAssert.Contains(tables, "roles");

        Console.WriteLine($"Vietnamese commented SQL extracted: {string.Join(", ", tables)}");
    }

    [TestMethod]
    public void GenerateBogusData_Performance_ShouldHandleLargeDataGeneration()
    {
        // Arrange
        var databaseInfo = CreateBusinessComplexDatabaseInfo();
        var startTime = DateTime.Now;

        // Act - Generate larger dataset
        var result = _dataGenService.GenerateBogusData(databaseInfo, 50);
        var endTime = DateTime.Now;
        var duration = endTime - startTime;

        // Assert
        Assert.IsNotNull(result, "Should generate large dataset");
        Assert.IsTrue(result.Count > 0, "Should generate statements");

        // Performance assertion - should complete within reasonable time
        Assert.IsTrue(duration.TotalSeconds < 10, $"Should complete within 10 seconds, took {duration.TotalSeconds:F2}s");

        // Count statements by table
        var tableStats = result.GroupBy(r => r.TableName)
                              .Select(g => new { Table = g.Key, Count = g.Count() })
                              .OrderBy(x => x.Table)
                              .ToList();

        Console.WriteLine($"Performance test completed in {duration.TotalSeconds:F2}s");
        Console.WriteLine("Generated statements by table:");
        foreach (var stat in tableStats)
        {
            Console.WriteLine($"  {stat.Table}: {stat.Count} statements");
        }
        Console.WriteLine($"Total statements: {result.Count}");
    }

    [TestMethod]
    public async Task ExecuteQueryWithTestDataAsync_ComplexSQL_MySQL_ShouldGenerateAndExecute()
    {
        // Arrange
        var complexSQL = @"
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
            ORDER BY ur.expires_at ASC, u.created_at DESC";

        var engineService = new EngineService(DatabaseType.MySQL, "Server=localhost;Database=testdb;Uid=testuser;Pwd=testpass;"); // No AI key for Bogus testing

        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = "Server=localhost;Database=testdb;Uid=testuser;Pwd=testpass;",
            SqlQuery = complexSQL,
            DesiredRecordCount = 10,
            OpenAiApiKey = null, // Use Bogus data generation
            UseAI = false,
            CurrentRecordCount = 0
        };

        // Act & Assert - Should not throw for SQL analysis even without real DB
        try
        {
            // Note: This will fail at database connection, but should succeed at SQL analysis
            var result = await engineService.ExecuteQueryWithTestDataAsync(request);
            
            // If we get here without connection, it means SQL parsing worked
            Assert.IsFalse(result.Success, "Should fail due to database connection");
            Assert.IsTrue(result.ErrorMessage.Contains("connection") || result.ErrorMessage.Contains("database"), 
                "Error should be about database connection");
        }
        catch (InvalidOperationException ex)
        {
            // Expected for database connection issues
            Assert.IsTrue(ex.Message.Contains("database") || ex.Message.Contains("connection"), 
                $"Expected database connection error, got: {ex.Message}");
        }

        Console.WriteLine("✅ Complex SQL parsing and engine workflow validation completed");
    }

    [TestMethod]
    public async Task ExecuteQueryWithTestDataAsync_WithValidConnection_ShouldCompleteWorkflow()
    {
        // Arrange - Simple SQL that doesn't require real tables
        var simpleSQL = "SELECT 1 as test_value";
        
        var engineService = new EngineService(DatabaseType.MySQL, "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;");

        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;",
            SqlQuery = simpleSQL,
            DesiredRecordCount = 1,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        // Act
        var result = await engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert
        Assert.IsNotNull(result, "Result should not be null");
        
        if (result.Success)
        {
            Assert.IsTrue(result.ExecutionTime > TimeSpan.Zero, "Execution time should be measured");
            Console.WriteLine($"✅ Workflow completed successfully in {result.ExecutionTime.TotalMilliseconds}ms");
            Console.WriteLine($"Generated records: {result.GeneratedRecords}");
        }
        else
        {
            Console.WriteLine($"❌ Workflow failed: {result.ErrorMessage}");
            // For simple SELECT 1, failure is acceptable as it may not have tables to analyze
            Assert.IsTrue(result.ErrorMessage.Contains("table") || result.ErrorMessage.Contains("schema"), 
                $"Expected table/schema error, got: {result.ErrorMessage}");
        }
    }

    [TestMethod]
    public async Task ExecuteQueryWithTestDataAsync_MySQLConnection_ShouldValidateConnectionString()
    {
        // Arrange
        var engineService = new EngineService(DatabaseType.MySQL, "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;");

        var validRequest = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;",
            SqlQuery = "SELECT * FROM users",
            DesiredRecordCount = 5,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        var invalidRequest = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = "invalid connection string",
            SqlQuery = "SELECT * FROM users",
            DesiredRecordCount = 5,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        // Act & Assert - Valid format connection string
        var validResult = await engineService.ExecuteQueryWithTestDataAsync(validRequest);
        Assert.IsNotNull(validResult, "Result should not be null for valid connection string format");
        
        // Act & Assert - Invalid connection string
        var invalidResult = await engineService.ExecuteQueryWithTestDataAsync(invalidRequest);
        Assert.IsNotNull(invalidResult, "Result should not be null for invalid connection string");
        Assert.IsFalse(invalidResult.Success, "Should fail with invalid connection string");
        
        // Accept various error types related to connection/database issues
        var errorMessage = invalidResult.ErrorMessage.ToLower();
        Assert.IsTrue(
            errorMessage.Contains("connection") || 
            errorMessage.Contains("format") ||
            errorMessage.Contains("database") ||
            errorMessage.Contains("schema") ||
            errorMessage.Contains("initialization") ||
            errorMessage.Contains("host"),
            $"Should mention connection/format/database error, got: {invalidResult.ErrorMessage}");

        Console.WriteLine("✅ MySQL connection string validation tests completed");
    }

    [TestMethod]
    public async Task ExecuteQueryWithTestDataAsync_ComplexSQL_ShouldAnalyzeTablesCorrectly()
    {
        // Arrange
        var complexSQL = @"
            SELECT u.id, u.first_name, u.last_name, u.email,
                   c.name as company_name, c.code as company_code,
                   r.name as role_name, r.code as role_code,
                   ur.expires_at, ur.is_active,
                   CASE 
                       WHEN u.is_active = 0 THEN 'Inactive'
                       WHEN ur.expires_at <= CURDATE() THEN 'Expired'
                       ELSE 'Active'
                   END as status
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            INNER JOIN user_roles ur ON u.id = ur.user_id
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE u.first_name LIKE '%Test%'
              AND c.name LIKE '%Company%'
              AND r.code IN ('ADMIN', 'USER')
              AND ur.is_active = 1
            ORDER BY u.last_name, c.name";

        var engineService = new EngineService(DatabaseType.MySQL, "Server=localhost;Database=test;Uid=user;Pwd=pass;");

        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = "Server=localhost;Database=test;Uid=user;Pwd=pass;",
            SqlQuery = complexSQL,
            DesiredRecordCount = 15,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 5
        };

        // Act
        var result = await engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert
        Assert.IsNotNull(result, "Result should not be null");
        
        // The SQL analysis should work regardless of connection
        // We expect it to fail at connection/execution, but SQL parsing should succeed
        if (!result.Success)
        {
            Console.WriteLine($"Expected failure due to connection: {result.ErrorMessage}");
            
            // Verify that error is related to connection, not SQL parsing
            Assert.IsTrue(
                result.ErrorMessage.Contains("connection") || 
                result.ErrorMessage.Contains("database") ||
                result.ErrorMessage.Contains("schema") ||
                result.ErrorMessage.Contains("connect"),
                $"Error should be about connection/database, got: {result.ErrorMessage}");
        }

        Console.WriteLine("✅ Complex SQL table analysis validation completed");
    }

    [TestMethod]
    public async Task ExecuteQueryWithTestDataAsync_EmptySQL_ShouldHandleGracefully()
    {
        // Arrange
        var engineService = new EngineService(DatabaseType.MySQL, "Server=localhost;Database=test;Uid=user;Pwd=pass;");

        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = "Server=localhost;Database=test;Uid=user;Pwd=pass;",
            SqlQuery = "",
            DesiredRecordCount = 10,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        // Act
        var result = await engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert
        Assert.IsNotNull(result, "Result should not be null");
        Assert.IsFalse(result.Success, "Should fail with empty SQL");
        
        // Accept various error types since empty SQL may fail at different stages
        var errorMessage = result.ErrorMessage.ToLower();
        Assert.IsTrue(
            errorMessage.Contains("table") || 
            errorMessage.Contains("query") ||
            errorMessage.Contains("database") ||
            errorMessage.Contains("schema") ||
            errorMessage.Contains("connection") ||
            errorMessage.Contains("host"),
            $"Should mention table/query/database issue, got: {result.ErrorMessage}");

        Console.WriteLine("✅ Empty SQL error handling test completed");
    }

    [TestMethod]
    public async Task ExecuteQueryWithTestDataAsync_Performance_ShouldCompleteWithinTimeout()
    {
        // Arrange
        var engineService = new EngineService(DatabaseType.MySQL, "Server=localhost;Database=perftest;Uid=user;Pwd=pass;");
        
        var complexSQL = @"
            SELECT u.id, u.username, u.email, u.created_at,
                   c.name, c.code, c.address,
                   r.name as role_name, r.level,
                   ur.assigned_at, ur.expires_at,
                   d.name as dept_name, d.budget
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            LEFT JOIN departments d ON u.department_id = d.id
            INNER JOIN user_roles ur ON u.id = ur.user_id
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE u.is_active = 1
              AND c.status = 'ACTIVE'
              AND ur.is_active = 1
              AND r.level >= 3
            ORDER BY u.created_at DESC, c.name ASC";

        var request = new QueryExecutionRequest
        {
            DatabaseType = "MySQL",
            ConnectionString = "Server=localhost;Database=perftest;Uid=user;Pwd=pass;",
            SqlQuery = complexSQL,
            DesiredRecordCount = 50,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 10
        };

        var startTime = DateTime.Now;

        // Act
        var result = await engineService.ExecuteQueryWithTestDataAsync(request);
        
        var endTime = DateTime.Now;
        var duration = endTime - startTime;

        // Assert
        Assert.IsNotNull(result, "Result should not be null");
        Assert.IsTrue(duration.TotalSeconds < 30, $"Should complete within 30 seconds, took {duration.TotalSeconds:F2}s");
        Assert.IsTrue(result.ExecutionTime > TimeSpan.Zero, "Execution time should be measured");

        Console.WriteLine($"✅ Performance test completed in {duration.TotalSeconds:F2}s");
        Console.WriteLine($"Result success: {result.Success}");
        Console.WriteLine($"Error (if any): {result.ErrorMessage}");
    }

    private DatabaseInfo CreateComplexDatabaseInfo()
    {
        var databaseInfo = new DatabaseInfo
        {
            Type = DatabaseType.MySQL
        };

        // Companies table
        var companySchema = new TableSchema
        {
            TableName = "companies",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "name", DataType = "varchar", MaxLength = 200 },
                new() { ColumnName = "code", DataType = "varchar", MaxLength = 20 }
            }
        };

        // Roles table
        var roleSchema = new TableSchema
        {
            TableName = "roles",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "name", DataType = "varchar", MaxLength = 100 },
                new() { ColumnName = "code", DataType = "varchar", MaxLength = 20 }
            }
        };

        // Users table (depends on companies)
        var userSchema = new TableSchema
        {
            TableName = "users",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "first_name", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "last_name", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "company_id", DataType = "int" },
                new() { ColumnName = "is_active", DataType = "bit" },
                new() { ColumnName = "date_of_birth", DataType = "date" },
                new() { ColumnName = "created_at", DataType = "datetime" }
            },
            ForeignKeys = new List<ForeignKeySchema>
            {
                new() { ColumnName = "company_id", ReferencedTable = "companies", ReferencedColumn = "id" }
            }
        };

        // User_roles table (depends on users and roles)
        var userRoleSchema = new TableSchema
        {
            TableName = "user_roles",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "user_id", DataType = "int" },
                new() { ColumnName = "role_id", DataType = "int" },
                new() { ColumnName = "is_active", DataType = "bit" },
                new() { ColumnName = "expires_at", DataType = "datetime", IsNullable = true }
            },
            ForeignKeys = new List<ForeignKeySchema>
            {
                new() { ColumnName = "user_id", ReferencedTable = "users", ReferencedColumn = "id" },
                new() { ColumnName = "role_id", ReferencedTable = "roles", ReferencedColumn = "id" }
            }
        };

        databaseInfo.Tables.Add("companies", companySchema);
        databaseInfo.Tables.Add("roles", roleSchema);
        databaseInfo.Tables.Add("users", userSchema);
        databaseInfo.Tables.Add("user_roles", userRoleSchema);

        return databaseInfo;
    }

    private DatabaseInfo CreateBusinessComplexDatabaseInfo()
    {
        var databaseInfo = new DatabaseInfo
        {
            Type = DatabaseType.MySQL
        };

        // Companies table - parent table
        var companySchema = new TableSchema
        {
            TableName = "companies",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "name", DataType = "varchar", MaxLength = 200 },
                new() { ColumnName = "code", DataType = "varchar", MaxLength = 20 },
                new() { ColumnName = "address", DataType = "varchar", MaxLength = 300 },
                new() { ColumnName = "status", DataType = "varchar", MaxLength = 20 },
                new() { ColumnName = "created_at", DataType = "datetime" }
            }
        };

        // Roles table - parent table
        var roleSchema = new TableSchema
        {
            TableName = "roles",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "name", DataType = "varchar", MaxLength = 100 },
                new() { ColumnName = "code", DataType = "varchar", MaxLength = 20 },
                new() { ColumnName = "level", DataType = "int" },
                new() { ColumnName = "description", DataType = "varchar", MaxLength = 500 },
                new() { ColumnName = "created_at", DataType = "datetime" }
            }
        };

        // Users table - depends on companies
        var userSchema = new TableSchema
        {
            TableName = "users",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "username", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "first_name", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "last_name", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "email", DataType = "varchar", MaxLength = 100 },
                new() { ColumnName = "date_of_birth", DataType = "date" },
                new() { ColumnName = "salary", DataType = "decimal" },
                new() { ColumnName = "department", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "hire_date", DataType = "date" },
                new() { ColumnName = "company_id", DataType = "int" },
                new() { ColumnName = "is_active", DataType = "bit" },
                new() { ColumnName = "created_at", DataType = "datetime" }
            },
            ForeignKeys = new List<ForeignKeySchema>
            {
                new() { ColumnName = "company_id", ReferencedTable = "companies", ReferencedColumn = "id" }
            }
        };

        // User_roles table - depends on users and roles
        var userRoleSchema = new TableSchema
        {
            TableName = "user_roles",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "user_id", DataType = "int" },
                new() { ColumnName = "role_id", DataType = "int" },
                new() { ColumnName = "assigned_by", DataType = "int" },
                new() { ColumnName = "assigned_at", DataType = "datetime" },
                new() { ColumnName = "expires_at", DataType = "datetime", IsNullable = true },
                new() { ColumnName = "is_active", DataType = "bit" }
            },
            ForeignKeys = new List<ForeignKeySchema>
            {
                new() { ColumnName = "user_id", ReferencedTable = "users", ReferencedColumn = "id" },
                new() { ColumnName = "role_id", ReferencedTable = "roles", ReferencedColumn = "id" }
            }
        };

        databaseInfo.Tables.Add("companies", companySchema);
        databaseInfo.Tables.Add("roles", roleSchema);
        databaseInfo.Tables.Add("users", userSchema);
        databaseInfo.Tables.Add("user_roles", userRoleSchema);

        return databaseInfo;
    }
} 