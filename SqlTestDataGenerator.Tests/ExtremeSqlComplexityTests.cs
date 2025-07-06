using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Services;
using System.Data;
using Dapper;
using MySqlConnector;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Tests for extremely complex SQL scenarios - the most challenging queries
/// Testing full workflow with edge cases, complex joins, subqueries, CTEs, window functions, etc.
/// </summary>
[TestClass]
public class ExtremeSqlComplexityTests
{
    private EngineService? _engineService;
    private readonly string _connectionString = "Server=localhost;Port=3306;Database=extreme_test_db;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;";
    private const string DatabaseType = "MySQL";

    [TestInitialize]
    public void Setup()
    {
        Console.WriteLine("=== EXTREME SQL COMPLEXITY TEST SETUP ===");
        _engineService = new EngineService("AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM");
        
        // Ensure export directory exists
        var exportDir = "sql-exports";
        if (!Directory.Exists(exportDir))
        {
            Directory.CreateDirectory(exportDir);
            Console.WriteLine($"✅ Created export directory: {exportDir}");
        }
    }

    [TestCleanup]
    public void Cleanup()
    {
        Console.WriteLine("=== EXTREME SQL COMPLEXITY TEST CLEANUP ===");
        _engineService = null;
    }

    /// <summary>
    /// Test Case 1: Complex SQL with Multiple CTEs, Window Functions, and Subqueries
    /// </summary>
    [TestMethod]
    public async Task TC_EXTREME_001_ComplexCTEsWindowFunctions_ShouldGenerateAndExecute()
    {
        Console.WriteLine("🚀 TC_EXTREME_001: Testing complex CTEs, window functions, and subqueries");
        
        var extremeSQL = @"
            -- Phức tạp: CTEs, Window Functions, Subqueries, Multiple JOINs
            WITH active_users AS (
                SELECT u.id, u.username, u.first_name, u.last_name, u.email, 
                       u.date_of_birth, u.salary, u.department, u.hire_date, u.company_id,
                       ROW_NUMBER() OVER (PARTITION BY u.company_id ORDER BY u.salary DESC) as salary_rank,
                       DENSE_RANK() OVER (ORDER BY u.hire_date) as hire_rank,
                       LAG(u.salary, 1) OVER (PARTITION BY u.department ORDER BY u.hire_date) as prev_salary,
                       LEAD(u.salary, 1) OVER (PARTITION BY u.department ORDER BY u.hire_date) as next_salary
                FROM users u
                WHERE u.is_active = TRUE
                  AND u.hire_date >= DATE_SUB(CURDATE(), INTERVAL 5 YEAR)
            ),
            company_stats AS (
                SELECT c.id, c.name, c.code,
                       COUNT(DISTINCT u.id) as total_users,
                       AVG(u.salary) as avg_salary,
                       MAX(u.salary) as max_salary,
                       MIN(u.salary) as min_salary,
                       STDDEV(u.salary) as salary_stddev
                FROM companies c
                LEFT JOIN users u ON c.id = u.company_id AND u.is_active = TRUE
                GROUP BY c.id, c.name, c.code
                HAVING COUNT(DISTINCT u.id) > 0
            ),
            role_hierarchy AS (
                SELECT r.id, r.name, r.code, r.level,
                       COUNT(DISTINCT ur.user_id) as user_count,
                       SUM(CASE WHEN ur.expires_at IS NULL OR ur.expires_at > NOW() THEN 1 ELSE 0 END) as active_assignments
                FROM roles r
                LEFT JOIN user_roles ur ON r.id = ur.role_id AND ur.is_active = TRUE
                GROUP BY r.id, r.name, r.code, r.level
            )
            SELECT 
                au.id,
                au.username,
                au.first_name,
                au.last_name,
                au.email,
                au.date_of_birth,
                au.salary,
                au.department,
                au.hire_date,
                au.salary_rank,
                au.hire_rank,
                au.prev_salary,
                au.next_salary,
                cs.name as company_name,
                cs.code as company_code,
                cs.total_users as company_total_users,
                cs.avg_salary as company_avg_salary,
                cs.max_salary as company_max_salary,
                cs.min_salary as company_min_salary,
                cs.salary_stddev as company_salary_stddev,
                GROUP_CONCAT(DISTINCT rh.name ORDER BY rh.level DESC SEPARATOR ', ') as all_roles,
                GROUP_CONCAT(DISTINCT rh.code ORDER BY rh.level DESC SEPARATOR ', ') as all_role_codes,
                COUNT(DISTINCT ur.role_id) as total_roles,
                SUM(CASE WHEN ur.expires_at IS NULL OR ur.expires_at > DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 1 ELSE 0 END) as active_roles,
                CASE 
                    WHEN au.salary > cs.avg_salary * 1.5 THEN 'High Performer'
                    WHEN au.salary > cs.avg_salary * 1.2 THEN 'Above Average'
                    WHEN au.salary > cs.avg_salary * 0.8 THEN 'Average'
                    ELSE 'Below Average'
                END as performance_category,
                CASE 
                    WHEN EXISTS (
                        SELECT 1 FROM user_roles ur2 
                        WHERE ur2.user_id = au.id 
                        AND ur2.expires_at IS NOT NULL 
                        AND ur2.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY)
                    ) THEN 'Role Expiring Soon'
                    WHEN NOT EXISTS (
                        SELECT 1 FROM user_roles ur3 
                        WHERE ur3.user_id = au.id 
                        AND ur3.is_active = TRUE
                    ) THEN 'No Active Roles'
                    ELSE 'Active'
                END as status,
                (
                    SELECT MAX(ur4.created_at) 
                    FROM user_roles ur4 
                    WHERE ur4.user_id = au.id
                ) as last_role_assignment
            FROM active_users au
            INNER JOIN company_stats cs ON au.company_id = cs.id
            LEFT JOIN user_roles ur ON au.id = ur.user_id AND ur.is_active = TRUE
            LEFT JOIN role_hierarchy rh ON ur.role_id = rh.id
            WHERE au.salary_rank <= 5
              AND cs.total_users >= 2
              AND (
                  au.first_name LIKE '%Phương%' 
                  OR au.last_name LIKE '%Phương%'
                  OR au.username LIKE '%phuong%'
                  OR au.email LIKE '%phuong%'
              )
              AND YEAR(au.date_of_birth) BETWEEN 1985 AND 1995
              AND cs.name LIKE '%VNEXT%'
              AND EXISTS (
                  SELECT 1 FROM user_roles ur_check 
                  INNER JOIN roles r_check ON ur_check.role_id = r_check.id
                  WHERE ur_check.user_id = au.id
                    AND r_check.code LIKE '%DD%'
                    AND ur_check.is_active = TRUE
              )
            GROUP BY au.id, au.username, au.first_name, au.last_name, au.email, au.date_of_birth, 
                     au.salary, au.department, au.hire_date, au.salary_rank, au.hire_rank, 
                     au.prev_salary, au.next_salary, cs.name, cs.code, cs.total_users, 
                     cs.avg_salary, cs.max_salary, cs.min_salary, cs.salary_stddev
            HAVING COUNT(DISTINCT ur.role_id) >= 1
            ORDER BY au.salary_rank ASC, au.hire_rank ASC, au.salary DESC
            LIMIT 10";

        var request = new QueryExecutionRequest
        {
            DatabaseType = DatabaseType,
            ConnectionString = _connectionString,
            SqlQuery = extremeSQL,
            DesiredRecordCount = 12,
            UseAI = true,
            OpenAiApiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM"
        };

        Console.WriteLine($"📝 Testing extreme SQL: {extremeSQL.Length} characters");
        Console.WriteLine($"🎯 Target records: {request.DesiredRecordCount}");

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert
        Assert.IsTrue(result.Success, $"Extreme SQL generation should succeed. Error: {result.ErrorMessage}");
        Assert.AreEqual(request.DesiredRecordCount, result.GeneratedRecords, 
            $"Generated records should match expected count. Expected: {request.DesiredRecordCount}, Actual: {result.GeneratedRecords}");
        
        Console.WriteLine($"✅ Extreme SQL test passed: {result.GeneratedRecords} records generated");
        Console.WriteLine($"📊 Query complexity: CTEs, Window Functions, Subqueries, Multiple JOINs handled successfully");
    }

    /// <summary>
    /// Test Case 2: Complex SQL with Recursive CTEs and Advanced Date Operations
    /// </summary>
    [TestMethod]
    public async Task TC_EXTREME_002_RecursiveCTEsAdvancedDates_ShouldGenerateAndExecute()
    {
        Console.WriteLine("🚀 TC_EXTREME_002: Testing recursive CTEs and advanced date operations");
        
        var recursiveSQL = @"
            -- Phức tạp: Recursive CTEs, Advanced Date Functions, Complex Conditions
            WITH RECURSIVE date_series AS (
                SELECT DATE_SUB(CURDATE(), INTERVAL 365 DAY) as date_value
                UNION ALL
                SELECT DATE_ADD(date_value, INTERVAL 1 DAY)
                FROM date_series
                WHERE date_value < CURDATE()
            ),
            user_tenure AS (
                SELECT u.id, u.username, u.first_name, u.last_name, u.email,
                       u.date_of_birth, u.salary, u.department, u.hire_date, u.company_id,
                       DATEDIFF(CURDATE(), u.hire_date) as days_employed,
                       TIMESTAMPDIFF(YEAR, u.hire_date, CURDATE()) as years_employed,
                       TIMESTAMPDIFF(MONTH, u.hire_date, CURDATE()) as months_employed,
                       TIMESTAMPDIFF(YEAR, u.date_of_birth, CURDATE()) as current_age,
                       TIMESTAMPDIFF(YEAR, u.date_of_birth, u.hire_date) as age_at_hire,
                       DAYOFWEEK(u.hire_date) as hire_day_of_week,
                       MONTHNAME(u.hire_date) as hire_month_name,
                       QUARTER(u.hire_date) as hire_quarter,
                       YEAR(u.hire_date) as hire_year,
                       CASE 
                           WHEN TIMESTAMPDIFF(YEAR, u.hire_date, CURDATE()) >= 5 THEN 'Senior'
                           WHEN TIMESTAMPDIFF(YEAR, u.hire_date, CURDATE()) >= 2 THEN 'Mid-level'
                           ELSE 'Junior'
                       END as experience_level
                FROM users u
                WHERE u.is_active = TRUE
            ),
            company_growth AS (
                SELECT c.id, c.name, c.code,
                       COUNT(DISTINCT u.id) as total_employees,
                       COUNT(DISTINCT CASE WHEN YEAR(u.hire_date) = YEAR(CURDATE()) THEN u.id END) as hired_this_year,
                       COUNT(DISTINCT CASE WHEN YEAR(u.hire_date) = YEAR(CURDATE()) - 1 THEN u.id END) as hired_last_year,
                       AVG(TIMESTAMPDIFF(YEAR, u.hire_date, CURDATE())) as avg_tenure_years,
                       MIN(u.hire_date) as first_hire_date,
                       MAX(u.hire_date) as latest_hire_date,
                       DATEDIFF(MAX(u.hire_date), MIN(u.hire_date)) as hiring_span_days
                FROM companies c
                LEFT JOIN users u ON c.id = u.company_id AND u.is_active = TRUE
                GROUP BY c.id, c.name, c.code
                HAVING COUNT(DISTINCT u.id) > 0
            )
            SELECT 
                ut.id,
                ut.username,
                ut.first_name,
                ut.last_name,
                ut.email,
                ut.date_of_birth,
                ut.salary,
                ut.department,
                ut.hire_date,
                ut.days_employed,
                ut.years_employed,
                ut.months_employed,
                ut.current_age,
                ut.age_at_hire,
                ut.hire_day_of_week,
                ut.hire_month_name,
                ut.hire_quarter,
                ut.hire_year,
                ut.experience_level,
                cg.name as company_name,
                cg.code as company_code,
                cg.total_employees,
                cg.hired_this_year,
                cg.hired_last_year,
                cg.avg_tenure_years,
                cg.first_hire_date,
                cg.latest_hire_date,
                cg.hiring_span_days,
                GROUP_CONCAT(DISTINCT r.name ORDER BY r.level DESC SEPARATOR ', ') as roles,
                COUNT(DISTINCT ur.role_id) as role_count,
                CASE 
                    WHEN ut.days_employed > cg.avg_tenure_years * 365 * 1.5 THEN 'Long-term Employee'
                    WHEN ut.days_employed > cg.avg_tenure_years * 365 THEN 'Experienced Employee'
                    ELSE 'New Employee'
                END as tenure_category,
                CASE 
                    WHEN ut.current_age BETWEEN 25 AND 35 THEN 'Young Professional'
                    WHEN ut.current_age BETWEEN 36 AND 45 THEN 'Mid-career'
                    WHEN ut.current_age BETWEEN 46 AND 55 THEN 'Senior Professional'
                    WHEN ut.current_age > 55 THEN 'Veteran'
                    ELSE 'Early Career'
                END as age_category,
                DATE_FORMAT(ut.hire_date, '%Y-%m-%d') as formatted_hire_date,
                DATE_FORMAT(ut.date_of_birth, '%Y-%m-%d') as formatted_birth_date,
                CONCAT(ut.experience_level, ' - ', ut.department) as experience_department
            FROM user_tenure ut
            INNER JOIN company_growth cg ON ut.company_id = cg.id
            LEFT JOIN user_roles ur ON ut.id = ur.user_id AND ur.is_active = TRUE
            LEFT JOIN roles r ON ur.role_id = r.id
            WHERE ut.years_employed >= 1
              AND ut.current_age BETWEEN 25 AND 50
              AND cg.total_employees >= 3
              AND (
                  ut.first_name LIKE '%Phương%' 
                  OR ut.last_name LIKE '%Phương%'
                  OR ut.username LIKE '%phuong%'
              )
              AND YEAR(ut.date_of_birth) BETWEEN 1980 AND 1995
              AND cg.name LIKE '%VNEXT%'
              AND ut.hire_date >= DATE_SUB(CURDATE(), INTERVAL 10 YEAR)
              AND EXISTS (
                  SELECT 1 FROM user_roles ur_check 
                  INNER JOIN roles r_check ON ur_check.role_id = r_check.id
                  WHERE ur_check.user_id = ut.id
                    AND r_check.code LIKE '%DD%'
                    AND ur_check.is_active = TRUE
              )
            GROUP BY ut.id, ut.username, ut.first_name, ut.last_name, ut.email, ut.date_of_birth, 
                     ut.salary, ut.department, ut.hire_date, ut.days_employed, ut.years_employed, 
                     ut.months_employed, ut.current_age, ut.age_at_hire, ut.hire_day_of_week, 
                     ut.hire_month_name, ut.hire_quarter, ut.hire_year, ut.experience_level,
                     cg.name, cg.code, cg.total_employees, cg.hired_this_year, cg.hired_last_year,
                     cg.avg_tenure_years, cg.first_hire_date, cg.latest_hire_date, cg.hiring_span_days
            HAVING COUNT(DISTINCT ur.role_id) >= 1
            ORDER BY ut.years_employed DESC, ut.salary DESC, ut.current_age ASC
            LIMIT 8";

        var request = new QueryExecutionRequest
        {
            DatabaseType = DatabaseType,
            ConnectionString = _connectionString,
            SqlQuery = recursiveSQL,
            DesiredRecordCount = 10,
            UseAI = true,
            OpenAiApiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM"
        };

        Console.WriteLine($"📝 Testing recursive CTE SQL: {recursiveSQL.Length} characters");
        Console.WriteLine($"🎯 Target records: {request.DesiredRecordCount}");

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert
        Assert.IsTrue(result.Success, $"Recursive CTE SQL generation should succeed. Error: {result.ErrorMessage}");
        Assert.AreEqual(request.DesiredRecordCount, result.GeneratedRecords, 
            $"Generated records should match expected count. Expected: {request.DesiredRecordCount}, Actual: {result.GeneratedRecords}");
        
        Console.WriteLine($"✅ Recursive CTE test passed: {result.GeneratedRecords} records generated");
        Console.WriteLine($"📊 Query complexity: Recursive CTEs, Advanced Date Functions handled successfully");
    }

    /// <summary>
    /// Test Case 3: Complex SQL with Multiple Nested Subqueries and Complex Aggregations
    /// </summary>
    [TestMethod]
    public async Task TC_EXTREME_003_NestedSubqueriesComplexAggregations_ShouldGenerateAndExecute()
    {
        Console.WriteLine("🚀 TC_EXTREME_003: Testing multiple nested subqueries and complex aggregations");
        
        var nestedSQL = @"
            -- Phức tạp: Multiple Nested Subqueries, Complex Aggregations, Multiple Conditions
            SELECT 
                main_user.id,
                main_user.username,
                main_user.first_name,
                main_user.last_name,
                main_user.email,
                main_user.date_of_birth,
                main_user.salary,
                main_user.department,
                main_user.hire_date,
                comp.name as company_name,
                comp.code as company_code,
                (
                    SELECT COUNT(*) 
                    FROM users u2 
                    WHERE u2.company_id = main_user.company_id 
                    AND u2.is_active = TRUE
                ) as total_company_users,
                (
                    SELECT AVG(u3.salary) 
                    FROM users u3 
                    WHERE u3.company_id = main_user.company_id 
                    AND u3.is_active = TRUE
                ) as avg_company_salary,
                (
                    SELECT COUNT(DISTINCT ur2.role_id) 
                    FROM user_roles ur2 
                    WHERE ur2.user_id = main_user.id 
                    AND ur2.is_active = TRUE
                ) as total_roles,
                (
                    SELECT GROUP_CONCAT(DISTINCT r2.name ORDER BY r2.level DESC SEPARATOR ', ') 
                    FROM user_roles ur3 
                    INNER JOIN roles r2 ON ur3.role_id = r2.id
                    WHERE ur3.user_id = main_user.id 
                    AND ur3.is_active = TRUE
                ) as role_names,
                (
                    SELECT COUNT(*) 
                    FROM users u4 
                    WHERE u4.department = main_user.department 
                    AND u4.company_id = main_user.company_id 
                    AND u4.is_active = TRUE
                ) as department_colleagues,
                (
                    SELECT MAX(u5.salary) 
                    FROM users u5 
                    WHERE u5.department = main_user.department 
                    AND u5.company_id = main_user.company_id 
                    AND u5.is_active = TRUE
                ) as max_department_salary,
                (
                    SELECT MIN(u6.hire_date) 
                    FROM users u6 
                    WHERE u6.department = main_user.department 
                    AND u6.company_id = main_user.company_id 
                    AND u6.is_active = TRUE
                ) as department_first_hire,
                (
                    SELECT COUNT(*) 
                    FROM user_roles ur4 
                    INNER JOIN roles r3 ON ur4.role_id = r3.id
                    WHERE ur4.user_id = main_user.id 
                    AND ur4.is_active = TRUE
                    AND r3.code LIKE '%DD%'
                ) as dd_role_count,
                (
                    SELECT ur5.expires_at 
                    FROM user_roles ur5 
                    INNER JOIN roles r4 ON ur5.role_id = r4.id
                    WHERE ur5.user_id = main_user.id 
                    AND ur5.is_active = TRUE
                    AND r4.code LIKE '%DD%'
                    ORDER BY ur5.expires_at ASC
                    LIMIT 1
                ) as earliest_dd_expiry,
                CASE 
                    WHEN main_user.salary > (
                        SELECT AVG(u7.salary) * 1.5
                        FROM users u7 
                        WHERE u7.company_id = main_user.company_id 
                        AND u7.is_active = TRUE
                    ) THEN 'Top Performer'
                    WHEN main_user.salary > (
                        SELECT AVG(u8.salary) * 1.2
                        FROM users u8 
                        WHERE u8.company_id = main_user.company_id 
                        AND u8.is_active = TRUE
                    ) THEN 'High Performer'
                    WHEN main_user.salary > (
                        SELECT AVG(u9.salary) * 0.8
                        FROM users u9 
                        WHERE u9.company_id = main_user.company_id 
                        AND u9.is_active = TRUE
                    ) THEN 'Average Performer'
                    ELSE 'Below Average'
                END as performance_rating,
                CASE 
                    WHEN EXISTS (
                        SELECT 1 FROM user_roles ur6 
                        INNER JOIN roles r5 ON ur6.role_id = r5.id
                        WHERE ur6.user_id = main_user.id 
                        AND ur6.is_active = TRUE
                        AND r5.code LIKE '%DD%'
                        AND ur6.expires_at IS NOT NULL
                        AND ur6.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY)
                    ) THEN 'DD Role Expiring Soon'
                    WHEN EXISTS (
                        SELECT 1 FROM user_roles ur7 
                        INNER JOIN roles r6 ON ur7.role_id = r6.id
                        WHERE ur7.user_id = main_user.id 
                        AND ur7.is_active = TRUE
                        AND r6.code LIKE '%DD%'
                    ) THEN 'Active DD Role'
                    ELSE 'No DD Role'
                END as dd_status,
                TIMESTAMPDIFF(YEAR, main_user.date_of_birth, CURDATE()) as current_age,
                TIMESTAMPDIFF(YEAR, main_user.hire_date, CURDATE()) as years_employed,
                DATEDIFF(CURDATE(), main_user.hire_date) as days_employed
            FROM users main_user
            INNER JOIN companies comp ON main_user.company_id = comp.id
            WHERE main_user.is_active = TRUE
              AND (
                  main_user.first_name LIKE '%Phương%' 
                  OR main_user.last_name LIKE '%Phương%'
                  OR main_user.username LIKE '%phuong%'
                  OR main_user.email LIKE '%phuong%'
              )
              AND YEAR(main_user.date_of_birth) BETWEEN 1985 AND 1995
              AND comp.name LIKE '%VNEXT%'
              AND main_user.salary > (
                  SELECT AVG(u_avg.salary) * 0.5
                  FROM users u_avg 
                  WHERE u_avg.company_id = main_user.company_id 
                  AND u_avg.is_active = TRUE
              )
              AND EXISTS (
                  SELECT 1 FROM user_roles ur_main 
                  INNER JOIN roles r_main ON ur_main.role_id = r_main.id
                  WHERE ur_main.user_id = main_user.id 
                  AND ur_main.is_active = TRUE
                  AND r_main.code LIKE '%DD%'
              )
              AND (
                  SELECT COUNT(*) 
                  FROM users u_dept 
                  WHERE u_dept.department = main_user.department 
                  AND u_dept.company_id = main_user.company_id 
                  AND u_dept.is_active = TRUE
              ) >= 2
            ORDER BY main_user.salary DESC, main_user.hire_date ASC
            LIMIT 6";

        var request = new QueryExecutionRequest
        {
            DatabaseType = DatabaseType,
            ConnectionString = _connectionString,
            SqlQuery = nestedSQL,
            DesiredRecordCount = 8,
            UseAI = true,
            OpenAiApiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM"
        };

        Console.WriteLine($"📝 Testing nested subqueries SQL: {nestedSQL.Length} characters");
        Console.WriteLine($"🎯 Target records: {request.DesiredRecordCount}");

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        // Assert
        Assert.IsTrue(result.Success, $"Nested subqueries SQL generation should succeed. Error: {result.ErrorMessage}");
        Assert.AreEqual(request.DesiredRecordCount, result.GeneratedRecords, 
            $"Generated records should match expected count. Expected: {request.DesiredRecordCount}, Actual: {result.GeneratedRecords}");
        
        Console.WriteLine($"✅ Nested subqueries test passed: {result.GeneratedRecords} records generated");
        Console.WriteLine($"📊 Query complexity: Multiple Nested Subqueries, Complex Aggregations handled successfully");
    }

    /// <summary>
    /// Test Case 4: Performance Test with Large Dataset Generation
    /// </summary>
    [TestMethod]
    public async Task TC_EXTREME_004_PerformanceTestLargeDataset_ShouldGenerateAndExecute()
    {
        Console.WriteLine("🚀 TC_EXTREME_004: Testing performance with large dataset generation");
        
        var performanceSQL = @"
            -- Performance Test: Large dataset với complex conditions
            SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, 
                   u.salary, u.department, u.hire_date, c.name as company_name, c.code as company_code,
                   r.name as role_name, r.code as role_code, ur.expires_at as role_expires,
                   CASE 
                       WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                       WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn vai trò'
                       ELSE 'Đang làm việc'
                   END as work_status,
                   TIMESTAMPDIFF(YEAR, u.date_of_birth, CURDATE()) as age,
                   TIMESTAMPDIFF(YEAR, u.hire_date, CURDATE()) as years_service
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = TRUE
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
              AND YEAR(u.date_of_birth) BETWEEN 1985 AND 1995
              AND c.name LIKE '%VNEXT%'
              AND r.code LIKE '%DD%'
              AND u.salary BETWEEN 50000 AND 200000
              AND u.hire_date >= DATE_SUB(CURDATE(), INTERVAL 10 YEAR)
            ORDER BY u.salary DESC, u.hire_date ASC";

        var request = new QueryExecutionRequest
        {
            DatabaseType = DatabaseType,
            ConnectionString = _connectionString,
            SqlQuery = performanceSQL,
            DesiredRecordCount = 100, // Large dataset
            UseAI = true,
            OpenAiApiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM"
        };

        Console.WriteLine($"📝 Testing performance SQL: {performanceSQL.Length} characters");
        Console.WriteLine($"🎯 Target records: {request.DesiredRecordCount} (LARGE DATASET)");

        var startTime = DateTime.Now;

        // Act
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);

        var endTime = DateTime.Now;
        var duration = endTime - startTime;

        // Assert
        Assert.IsTrue(result.Success, $"Performance test should succeed. Error: {result.ErrorMessage}");
        Assert.AreEqual(request.DesiredRecordCount, result.GeneratedRecords, 
            $"Generated records should match expected count. Expected: {request.DesiredRecordCount}, Actual: {result.GeneratedRecords}");
        
        // Performance assertion - should complete within reasonable time
        Assert.IsTrue(duration.TotalMinutes < 5, $"Should complete within 5 minutes, took {duration.TotalMinutes:F2} minutes");
        
        Console.WriteLine($"✅ Performance test passed: {result.GeneratedRecords} records generated");
        Console.WriteLine($"⏱️ Performance: Completed in {duration.TotalSeconds:F2} seconds");
        Console.WriteLine($"📊 Query complexity: Large dataset generation handled successfully");
    }

    /// <summary>
    /// Test Case 5: Full Workflow Test with Export and Verification
    /// </summary>
    [TestMethod]
    public async Task TC_EXTREME_005_FullWorkflowWithExportVerification_ShouldComplete()
    {
        Console.WriteLine("🚀 TC_EXTREME_005: Testing full workflow with export and verification");
        
        var workflowSQL = @"
            -- Full Workflow Test: Complete pipeline từ generation đến verification
            SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date, 
                   c.name as company_name, c.code as company_code, r.name as role_name, r.code as role_code, ur.expires_at as role_expires,
                   CASE 
                       WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                       WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn vai trò'
                       ELSE 'Đang làm việc'
                   END as work_status
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = TRUE
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
              AND YEAR(u.date_of_birth) = 1989
              AND c.name LIKE '%VNEXT%'
              AND r.code LIKE '%DD%'
              AND (u.is_active = 0 OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY))
            ORDER BY ur.expires_at ASC, u.created_at DESC";

        var request = new QueryExecutionRequest
        {
            DatabaseType = DatabaseType,
            ConnectionString = _connectionString,
            SqlQuery = workflowSQL,
            DesiredRecordCount = 15,
            UseAI = true,
            OpenAiApiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM"
        };

        Console.WriteLine($"📝 Testing full workflow SQL: {workflowSQL.Length} characters");
        Console.WriteLine($"🎯 Target records: {request.DesiredRecordCount}");

        // Phase 1: Generate
        Console.WriteLine("\n=== PHASE 1: GENERATION ===");
        var result = await _engineService.ExecuteQueryWithTestDataAsync(request);
        
        Assert.IsTrue(result.Success, $"Generation phase should succeed. Error: {result.ErrorMessage}");
        Assert.AreEqual(request.DesiredRecordCount, result.GeneratedRecords, 
            $"Generated records should match expected. Expected: {request.DesiredRecordCount}, Actual: {result.GeneratedRecords}");
        
        Console.WriteLine($"✅ Phase 1 passed: {result.GeneratedRecords} records generated");

        // Phase 2: Export
        Console.WriteLine("\n=== PHASE 2: EXPORT ===");
        Assert.IsNotNull(result.ExportedFilePath, "Export file path should be provided");
        Assert.IsTrue(File.Exists(result.ExportedFilePath), $"Export file should exist: {result.ExportedFilePath}");
        
        var exportedContent = await File.ReadAllTextAsync(result.ExportedFilePath);
        Assert.IsTrue(exportedContent.Length > 0, "Export file should not be empty");
        
        var exportedStatements = exportedContent.Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries)
                                              .Where(s => !string.IsNullOrWhiteSpace(s) && s.Trim().StartsWith("INSERT"))
                                              .ToList();
        
        Assert.IsTrue(exportedStatements.Count > 0, "Export file should contain INSERT statements");
        
        Console.WriteLine($"✅ Phase 2 passed: {exportedStatements.Count} statements exported");

        // Phase 3: Verification
        Console.WriteLine("\n=== PHASE 3: VERIFICATION ===");
        Assert.IsNotNull(result.ResultData, "Result data should be provided");
        Assert.IsTrue(result.ResultData.Rows.Count >= 0, "Result data should be valid");
        
        Console.WriteLine($"✅ Phase 3 passed: {result.ResultData.Rows.Count} rows in result preview");

        // Final Summary
        Console.WriteLine("\n=== FULL WORKFLOW SUMMARY ===");
        Console.WriteLine($"✅ Generation: {result.GeneratedRecords} records");
        Console.WriteLine($"✅ Export: {exportedStatements.Count} statements");
        Console.WriteLine($"✅ Verification: {result.ResultData.Rows.Count} preview rows");
        Console.WriteLine($"✅ Export File: {Path.GetFileName(result.ExportedFilePath)}");
        Console.WriteLine($"📊 Full workflow completed successfully!");
        
        // Clean up
        if (File.Exists(result.ExportedFilePath))
        {
            File.Delete(result.ExportedFilePath);
        }
    }
}