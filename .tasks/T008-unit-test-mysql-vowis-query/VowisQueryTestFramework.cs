using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlConnector;
using Dapper;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Data;

namespace MySqlVowisQueryTests;

[TestClass]
public class VowisQueryTestFramework
{
    protected static IConfiguration? Configuration { get; private set; }
    protected static ILogger? Logger { get; private set; }
    protected static string? ConnectionString { get; private set; }

    // Original query được test
    protected static readonly string VowisQuery = @"
        -- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
        SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date,
               c.NAME AS company_name, c.code AS company_code,
               r.NAME AS role_name, r.code AS role_code,
               ur.expires_at AS role_expires,
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

    [ClassInitialize]
    public static void GlobalSetup(TestContext context)
    {
        try
        {
            // Setup Configuration
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json", optional: false)
                .Build();

            // Setup Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            Logger = Log.Logger;

            // Get connection string
            ConnectionString = Configuration.GetConnectionString("MySQL");

            Logger.Information("=== VOWIS QUERY UNIT TEST FRAMEWORK INITIALIZED ===");
            Logger.Information("Connection String: {ConnectionString}", ConnectionString?.Substring(0, Math.Min(50, ConnectionString.Length)) + "...");
            Logger.Information("Test Context: {TestName}", context.TestName);

            // Verify database connection
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            Logger.Information("✅ Database connection verified successfully");
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, "❌ Failed to initialize test framework");
            throw;
        }
    }

    [ClassCleanup]
    public static void GlobalCleanup()
    {
        try
        {
            Logger?.Information("=== CLEANING UP TEST FRAMEWORK ===");
            
            // Cleanup test data  
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            
            var cleanupScript = @"
                DELETE FROM user_roles WHERE user_id >= 1000;
                DELETE FROM users WHERE id >= 1000;
                DELETE FROM companies WHERE id >= 1000;
                DELETE FROM roles WHERE id >= 1000;";
                
            connection.Execute(cleanupScript);
            Logger?.Information("✅ Test data cleaned up successfully");
            
            Log.CloseAndFlush();
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, "❌ Error during cleanup");
        }
    }

    [TestInitialize]
    public void Setup()
    {
        Logger?.Information("=== STARTING TEST: {TestMethod} ===", TestContext.TestName);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Logger?.Information("=== COMPLETED TEST: {TestMethod} ===", TestContext.TestName);
    }

    public TestContext TestContext { get; set; } = null!;

    #region Helper Methods

    /// <summary>
    /// Execute SQL script file
    /// </summary>
    protected async Task<int> ExecuteSqlFileAsync(string filePath)
    {
        var sql = await File.ReadAllTextAsync(filePath);
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        Logger?.Information("Executing SQL file: {FilePath}", filePath);
        var result = await connection.ExecuteAsync(sql);
        Logger?.Information("SQL file executed successfully, affected rows: {AffectedRows}", result);
        
        return result;
    }

    /// <summary>
    /// Execute query and return results as dynamic objects
    /// </summary>
    protected async Task<IEnumerable<dynamic>> ExecuteQueryAsync(string query)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        Logger?.Information("Executing query: {Query}", query.Substring(0, Math.Min(100, query.Length)) + "...");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var results = await connection.QueryAsync(query);
        
        stopwatch.Stop();
        Logger?.Information("Query executed in {ElapsedMs}ms, returned {RowCount} rows", 
            stopwatch.ElapsedMilliseconds, results.Count());
        
        return results;
    }

    /// <summary>
    /// Execute query and return strongly typed results
    /// </summary>
    protected async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string query)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        Logger?.Information("Executing typed query: {Query}", query.Substring(0, Math.Min(100, query.Length)) + "...");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var results = await connection.QueryAsync<T>(query);
        
        stopwatch.Stop();
        Logger?.Information("Typed query executed in {ElapsedMs}ms, returned {RowCount} rows", 
            stopwatch.ElapsedMilliseconds, results.Count());
        
        return results;
    }

    /// <summary>
    /// Execute scalar query (for counts, etc.)
    /// </summary>
    protected async Task<T> ExecuteScalarAsync<T>(string query)
    {
        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        Logger?.Information("Executing scalar query: {Query}", query);
        var result = await connection.QuerySingleAsync<T>(query);
        Logger?.Information("Scalar query result: {Result}", result);
        
        return result;
    }

    /// <summary>
    /// Verify database tables exist and have expected structure
    /// </summary>
    protected async Task VerifyTablesExistAsync()
    {
        var checkTablesQuery = @"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = DATABASE() 
              AND table_name IN ('users', 'companies', 'roles', 'user_roles')
            ORDER BY table_name";
        
        var tables = await ExecuteQueryAsync<string>(checkTablesQuery);
        var tableList = tables.ToList();
        
        Logger?.Information("Found tables: {Tables}", string.Join(", ", tableList));
        
        Assert.AreEqual(4, tableList.Count, "Expected 4 tables: users, companies, roles, user_roles");
        Assert.IsTrue(tableList.Contains("companies"), "Table 'companies' not found");
        Assert.IsTrue(tableList.Contains("roles"), "Table 'roles' not found");
        Assert.IsTrue(tableList.Contains("users"), "Table 'users' not found");
        Assert.IsTrue(tableList.Contains("user_roles"), "Table 'user_roles' not found");
    }

    /// <summary>
    /// Setup test data by executing setup script
    /// </summary>
    protected async Task SetupTestDataAsync()
    {
        var setupFile = Path.Combine(Directory.GetCurrentDirectory(), "TestDataSetup.sql");
        if (!File.Exists(setupFile))
        {
            throw new FileNotFoundException($"Test data setup file not found: {setupFile}");
        }

        Logger?.Information("Setting up test data from: {SetupFile}", setupFile);
        await ExecuteSqlFileAsync(setupFile);
        
        // Verify test data was inserted
        var testUserCount = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE id >= 1000");
        var testCompanyCount = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM companies WHERE id >= 1000");
        var testRoleCount = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM roles WHERE id >= 1000");
        var testUserRoleCount = await ExecuteScalarAsync<int>("SELECT COUNT(*) FROM user_roles WHERE user_id >= 1000");
        
        Logger?.Information("Test data counts - Users: {Users}, Companies: {Companies}, Roles: {Roles}, UserRoles: {UserRoles}",
            testUserCount, testCompanyCount, testRoleCount, testUserRoleCount);
        
        Assert.IsTrue(testUserCount > 0, "No test users were inserted");
        Assert.IsTrue(testCompanyCount > 0, "No test companies were inserted");
        Assert.IsTrue(testRoleCount > 0, "No test roles were inserted");
        Assert.IsTrue(testUserRoleCount > 0, "No test user roles were inserted");
    }

    /// <summary>
    /// Validate query result structure and data types
    /// </summary>
    protected void ValidateQueryResultStructure(dynamic result)
    {
        var resultDict = (IDictionary<string, object>)result;
        
        // Required columns
        var requiredColumns = new[]
        {
            "id", "username", "first_name", "last_name", "email", "date_of_birth",
            "salary", "department", "hire_date", "company_name", "company_code",
            "role_name", "role_code", "role_expires", "work_status"
        };

        foreach (var column in requiredColumns)
        {
            Assert.IsTrue(resultDict.ContainsKey(column), $"Required column '{column}' not found in result");
        }

        // Validate work_status values
        var workStatus = resultDict["work_status"]?.ToString();
        var validStatuses = new[] { "Đã nghỉ việc", "Sắp hết hạn vai trò", "Đang làm việc" };
        Assert.IsTrue(validStatuses.Contains(workStatus), $"Invalid work_status: {workStatus}");

        Logger?.Information("✅ Query result structure validation passed");
    }

    /// <summary>
    /// Validate business logic conditions
    /// </summary>
    protected void ValidateBusinessLogic(dynamic result)
    {
        var resultDict = (IDictionary<string, object>)result;
        
        var firstName = resultDict["first_name"]?.ToString();
        var lastName = resultDict["last_name"]?.ToString();
        var dateOfBirth = Convert.ToDateTime(resultDict["date_of_birth"]);
        var companyName = resultDict["company_name"]?.ToString();
        var roleCode = resultDict["role_code"]?.ToString();
        var workStatus = resultDict["work_status"]?.ToString();

        // Check name contains "Phương"
        var hasNamePhuong = firstName?.Contains("Phương") == true || lastName?.Contains("Phương") == true;
        Assert.IsTrue(hasNamePhuong, $"Name should contain 'Phương' - First: {firstName}, Last: {lastName}");

        // Check birth year is 1989
        Assert.AreEqual(1989, dateOfBirth.Year, $"Birth year should be 1989, got: {dateOfBirth.Year}");

        // Check company contains "VNEXT"
        Assert.IsTrue(companyName?.Contains("VNEXT") == true, $"Company should contain 'VNEXT', got: {companyName}");

        // Check role code contains "DD"
        Assert.IsTrue(roleCode?.Contains("DD") == true, $"Role code should contain 'DD', got: {roleCode}");

        // Check work status indicates leaving soon or already left
        var leavingStatuses = new[] { "Đã nghỉ việc", "Sắp hết hạn vai trò" };
        Assert.IsTrue(leavingStatuses.Contains(workStatus), $"Work status should indicate leaving: {workStatus}");

        Logger?.Information("✅ Business logic validation passed for user: {FirstName} {LastName}", firstName, lastName);
    }

    #endregion
} 