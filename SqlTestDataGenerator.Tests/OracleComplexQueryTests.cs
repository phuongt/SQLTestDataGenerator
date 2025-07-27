using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using System;
using System.Linq;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Integration tests với real Oracle database cho complex query generation
/// Tương tự như MySQL tests nhưng với Oracle syntax
/// </summary>
[TestClass]
[TestCategory("RealOracle")]
public class OracleComplexQueryTests
{
    // Real Oracle connection - default
    private const string REAL_ORACLE_CONNECTION = "Data Source=localhost:1521/XE;User Id=system;Password=22092012;Connection Timeout=120;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;";
    
    private EngineService _engineService = null!;

    [TestInitialize]
    public void Setup()
    {
        _engineService = new EngineService(DatabaseType.Oracle, REAL_ORACLE_CONNECTION);
    }

    [TestMethod]
    [Timeout(180000)] // 3 minutes timeout
    public async Task ExecuteQueryWithTestDataAsync_RealOracle_SimpleUsers_ShouldWork()
    {
        // Arrange
        var simpleSQL = @"
            SELECT * FROM users 
            WHERE is_active = 1 
            ORDER BY created_at DESC 
            FETCH FIRST 10 ROWS ONLY";

        Console.WriteLine("=== TEST: Simple Users Query với Real Oracle ===");
        Console.WriteLine($"Connection: Oracle XE");
        Console.WriteLine($"SQL: {simpleSQL.Trim()}");
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "Oracle",
            ConnectionString = REAL_ORACLE_CONNECTION,
            SqlQuery = simpleSQL,
            DesiredRecordCount = 5,
            OpenAiApiKey = null, // Use Bogus generation
            UseAI = false,
            CurrentRecordCount = 0
        };

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert
        Console.WriteLine($"\nSimple Users Result:");
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");
        Console.WriteLine($"Generated INSERTs: {result.GeneratedInserts.Count}");
        
        if (result.Success)
        {
            Console.WriteLine($"Query Result Rows: {result.ResultData?.Rows.Count ?? 0}");
            Assert.IsTrue(result.GeneratedInserts.Count > 0, "Should generate INSERT statements");
            Console.WriteLine("✅ SUCCESS: Simple users test with real Oracle passed");
        }
        else
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
        }
    }

    [TestMethod]
    [Timeout(180000)] // 3 minutes timeout
    public async Task ExecuteQueryWithTestDataAsync_RealOracle_ComplexJoin_ShouldWork()
    {
        // Arrange
        var complexSQL = @"
            SELECT u.id, u.username, u.email, u.created_at,
                   c.name as company_name, c.code as company_code,
                   r.name as role_name, r.code as role_code,
                   ur.assigned_at, ur.expires_at
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            INNER JOIN user_roles ur ON u.id = ur.user_id
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE u.is_active = 1 
              AND ur.is_active = 1
            ORDER BY u.created_at DESC
            FETCH FIRST 20 ROWS ONLY";

        Console.WriteLine("=== TEST: Complex JOIN với Real Oracle ===");
        Console.WriteLine($"Tables: users, companies, user_roles, roles");
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "Oracle",
            ConnectionString = REAL_ORACLE_CONNECTION,
            SqlQuery = complexSQL,
            DesiredRecordCount = 10,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert
        Console.WriteLine($"\nComplex JOIN Result:");
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");
        Console.WriteLine($"Generated INSERTs: {result.GeneratedInserts.Count}");
        
        if (result.Success)
        {
            Console.WriteLine($"Query Result Rows: {result.ResultData?.Rows.Count ?? 0}");
            
            if (result.GeneratedInserts.Count > 0)
            {
                var insertsByTable = result.GeneratedInserts
                    .GroupBy(sql => ExtractTableFromInsert(sql))
                    .ToDictionary(g => g.Key, g => g.Count());
                
                Console.WriteLine("INSERT statements by table:");
                foreach (var kvp in insertsByTable)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value} statements");
                }
                
                Assert.IsTrue(result.GeneratedInserts.Count > 0, "Should generate INSERT statements");
            }
            
            Console.WriteLine("✅ SUCCESS: Complex JOIN test with real Oracle passed");
        }
        else
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
        }
    }

    [TestMethod]
    [Timeout(180000)] // 3 minutes timeout
    public async Task ExecuteQueryWithTestDataAsync_RealOracle_BusinessLogic_ShouldWork()
    {
        // Arrange - Business logic query tương tự MySQL nhưng với Oracle syntax
        var businessSQL = @"
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
            ORDER BY ur.expires_at ASC, u.created_at DESC
            FETCH FIRST 15 ROWS ONLY";

        Console.WriteLine("=== TEST: Business Logic Query với Real Oracle ===");
        Console.WriteLine($"SQL: Business logic với Vietnamese comments");
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "Oracle",
            ConnectionString = REAL_ORACLE_CONNECTION,
            SqlQuery = businessSQL,
            DesiredRecordCount = 10,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert
        Console.WriteLine($"\nBusiness Logic Result:");
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");
        Console.WriteLine($"Generated INSERTs: {result.GeneratedInserts.Count}");
        
        if (result.Success)
        {
            Console.WriteLine($"Query Result Rows: {result.ResultData?.Rows.Count ?? 0}");
            Assert.IsTrue(result.GeneratedInserts.Count > 0, "Should generate INSERT statements");
            Console.WriteLine("✅ SUCCESS: Business logic test with real Oracle passed");
        }
        else
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
        }
    }

    [TestMethod]
    [Timeout(180000)] // 3 minutes timeout
    public async Task ExecuteQueryWithTestDataAsync_RealOracle_DateFunctions_ShouldWork()
    {
        // Arrange - Date functions với Oracle syntax
        var dateSQL = @"
            SELECT u.id, u.first_name, u.last_name, u.hire_date,
                   EXTRACT(YEAR FROM u.hire_date) as hire_year,
                   EXTRACT(MONTH FROM u.hire_date) as hire_month,
                   ADD_MONTHS(u.hire_date, 12) as one_year_later,
                   MONTHS_BETWEEN(SYSDATE, u.hire_date) as months_employed,
                   CASE 
                       WHEN MONTHS_BETWEEN(SYSDATE, u.hire_date) >= 60 THEN 'Senior'
                       WHEN MONTHS_BETWEEN(SYSDATE, u.hire_date) >= 24 THEN 'Mid-level'
                       ELSE 'Junior'
                   END as experience_level
            FROM users u
            WHERE u.hire_date >= SYSDATE - INTERVAL '5' YEAR
              AND u.is_active = 1
            ORDER BY u.hire_date DESC
            FETCH FIRST 15 ROWS ONLY";

        Console.WriteLine("=== TEST: Date Functions với Real Oracle ===");
        Console.WriteLine($"SQL: Oracle date functions (EXTRACT, ADD_MONTHS, MONTHS_BETWEEN)");
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "Oracle",
            ConnectionString = REAL_ORACLE_CONNECTION,
            SqlQuery = dateSQL,
            DesiredRecordCount = 8,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert
        Console.WriteLine($"\nDate Functions Result:");
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");
        Console.WriteLine($"Generated INSERTs: {result.GeneratedInserts.Count}");
        
        if (result.Success)
        {
            Console.WriteLine($"Query Result Rows: {result.ResultData?.Rows.Count ?? 0}");
            Assert.IsTrue(result.GeneratedInserts.Count > 0, "Should generate INSERT statements");
            Console.WriteLine("✅ SUCCESS: Date functions test with real Oracle passed");
        }
        else
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
        }
    }

    [TestMethod]
    [Timeout(300000)] // 5 minutes timeout for performance test
    public async Task ExecuteQueryWithTestDataAsync_RealOracle_Performance_ShouldWork()
    {
        // Arrange - Performance test với large dataset
        var performanceSQL = @"
            SELECT u.id, u.username, u.first_name, u.last_name, u.email,
                   c.name as company_name, c.code as company_code,
                   r.name as role_name, r.code as role_code,
                   ur.assigned_at, ur.expires_at,
                   COUNT(*) OVER (PARTITION BY c.id) as company_user_count,
                   ROW_NUMBER() OVER (PARTITION BY c.id ORDER BY u.created_at) as user_rank_in_company
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            INNER JOIN user_roles ur ON u.id = ur.user_id
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE u.is_active = 1 
              AND ur.is_active = 1
              AND u.created_at >= SYSDATE - INTERVAL '2' YEAR
            ORDER BY c.name, u.created_at DESC
            FETCH FIRST 50 ROWS ONLY";

        Console.WriteLine("=== TEST: Performance với Real Oracle ===");
        Console.WriteLine($"SQL: Large dataset với window functions");
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "Oracle",
            ConnectionString = REAL_ORACLE_CONNECTION,
            SqlQuery = performanceSQL,
            DesiredRecordCount = 25,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);
        stopwatch.Stop();

        // Assert
        Console.WriteLine($"\nPerformance Result:");
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");
        Console.WriteLine($"Generated INSERTs: {result.GeneratedInserts.Count}");
        Console.WriteLine($"Execution Time: {stopwatch.ElapsedMilliseconds}ms");
        
        if (result.Success)
        {
            Console.WriteLine($"Query Result Rows: {result.ResultData?.Rows.Count ?? 0}");
            Assert.IsTrue(result.GeneratedInserts.Count > 0, "Should generate INSERT statements");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 120000, "Should complete within 2 minutes");
            Console.WriteLine("✅ SUCCESS: Performance test with real Oracle passed");
        }
        else
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
        }
    }

    [TestMethod]
    [Timeout(180000)] // 3 minutes timeout
    public async Task ExecuteQueryWithTestDataAsync_RealOracle_CTE_ShouldWork()
    {
        // Arrange - CTE (Common Table Expression) với Oracle syntax
        var cteSQL = @"
            WITH active_users AS (
                SELECT u.id, u.username, u.first_name, u.last_name, u.email, 
                       u.salary, u.department, u.hire_date, u.company_id
                FROM users u
                WHERE u.is_active = 1
                  AND u.hire_date >= SYSDATE - INTERVAL '3' YEAR
            ),
            company_stats AS (
                SELECT c.id, c.name, c.code,
                       COUNT(DISTINCT au.id) as total_users,
                       AVG(au.salary) as avg_salary,
                       MAX(au.salary) as max_salary,
                       MIN(au.salary) as min_salary
                FROM companies c
                LEFT JOIN active_users au ON c.id = au.company_id
                GROUP BY c.id, c.name, c.code
                HAVING COUNT(DISTINCT au.id) > 0
            )
            SELECT au.id, au.username, au.first_name, au.last_name,
                   au.salary, au.department, au.hire_date,
                   cs.name as company_name, cs.code as company_code,
                   cs.total_users, cs.avg_salary, cs.max_salary, cs.min_salary,
                   CASE 
                       WHEN au.salary >= cs.avg_salary * 1.2 THEN 'High'
                       WHEN au.salary >= cs.avg_salary * 0.8 THEN 'Medium'
                       ELSE 'Low'
                   END as salary_level
            FROM active_users au
            INNER JOIN company_stats cs ON au.company_id = cs.id
            ORDER BY cs.avg_salary DESC, au.salary DESC
            FETCH FIRST 20 ROWS ONLY";

        Console.WriteLine("=== TEST: CTE với Real Oracle ===");
        Console.WriteLine($"SQL: Common Table Expressions với window functions");
        
        var request = new QueryExecutionRequest
        {
            DatabaseType = "Oracle",
            ConnectionString = REAL_ORACLE_CONNECTION,
            SqlQuery = cteSQL,
            DesiredRecordCount = 12,
            OpenAiApiKey = null,
            UseAI = false,
            CurrentRecordCount = 0
        };

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert
        Console.WriteLine($"\nCTE Result:");
        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Generated Records: {result.GeneratedRecords}");
        Console.WriteLine($"Generated INSERTs: {result.GeneratedInserts.Count}");
        
        if (result.Success)
        {
            Console.WriteLine($"Query Result Rows: {result.ResultData?.Rows.Count ?? 0}");
            Assert.IsTrue(result.GeneratedInserts.Count > 0, "Should generate INSERT statements");
            Console.WriteLine("✅ SUCCESS: CTE test with real Oracle passed");
        }
        else
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
        }
    }

    private static string ExtractTableFromInsert(string insertSql)
    {
        // Simple extraction - look for "INSERT INTO table_name"
        var match = System.Text.RegularExpressions.Regex.Match(insertSql, @"INSERT\s+INTO\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "unknown";
    }
} 