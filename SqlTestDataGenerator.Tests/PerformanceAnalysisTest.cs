using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Services;
using System.Collections.Generic;
using System.Linq;

namespace SqlTestDataGenerator.Tests
{
    [TestClass]
    public class PerformanceAnalysisTest
    {
        private readonly Stopwatch _totalStopwatch = new Stopwatch();

        [TestInitialize]
        public void Setup()
        {
            _totalStopwatch.Start();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🚀 Starting Performance Analysis Test");
            
            // Initialize centralized logging
            CentralizedLoggingManager.Initialize();
        }

        [TestMethod]
        public void Analyze_Performance_Bottlenecks()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔍 Starting Performance Bottleneck Analysis");
            
            var complexSql = @"
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
ORDER BY ur.expires_at ASC, u.created_at DESC";

            // Step 1: Test SQL Parsing Performance
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📝 Step 1: Testing SQL Parsing Performance");
            var parsingStopwatch = Stopwatch.StartNew();
            var queryParser = new SqlQueryParserV3();
            var parsedQuery = queryParser.ParseQuery(complexSql);
            parsingStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ SQL Parsing: {parsingStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Parsed: {parsedQuery.TableRequirements.Count} tables, {parsedQuery.WhereConditions.Count} WHERE conditions");

            // Step 2: Test Constraint Extraction Performance
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔗 Step 2: Testing Constraint Extraction Performance");
            var constraintStopwatch = Stopwatch.StartNew();
            var constraintExtractor = new ComprehensiveConstraintExtractor();
            var constraints = constraintExtractor.ExtractAllConstraints(complexSql);
            constraintStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Constraint Extraction: {constraintStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Extracted: {constraints.LikePatterns.Count} LIKE patterns, {constraints.JoinConstraints.Count} JOIN constraints");

            // Step 3: Test Data Generation Performance (Bottleneck Analysis)
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🤖 Step 3: Testing Data Generation Performance");
            var generationStopwatch = Stopwatch.StartNew();
            
            // Create mock database info for testing
            var databaseInfo = CreateMockDatabaseInfo();
            
            // Test different generation approaches
            var dataGenService = new DataGenService();
            
            // 3.1: Test Bogus Generation Performance
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔄 3.1: Testing Bogus Generation");
            var bogusStopwatch = Stopwatch.StartNew();
            var bogusStatements = dataGenService.GenerateBogusData(databaseInfo, 5, complexSql, constraints, null);
            bogusStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Bogus Generation: {bogusStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Generated: {bogusStatements.Count} statements");

            // 3.2: Test Coordinated Generation Performance
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔄 3.2: Testing Coordinated Generation");
            var coordinatedStopwatch = Stopwatch.StartNew();
            var coordinatedGenerator = new CoordinatedDataGenerator();
            var coordinatedTask = coordinatedGenerator.GenerateCoordinatedDataAsync(
                databaseInfo, complexSql, 5, "Oracle", "");
            coordinatedTask.Wait(); // Wait for completion
            var coordinatedStatements = coordinatedTask.Result;
            coordinatedStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Coordinated Generation: {coordinatedStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Generated: {coordinatedStatements.Count} statements");

            generationStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Total Data Generation: {generationStopwatch.Elapsed.TotalSeconds:F2}s");

            // Step 4: Test AI Service Performance
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🤖 Step 4: Testing AI Service Performance");
            var aiStopwatch = Stopwatch.StartNew();
            var geminiService = new GeminiAIDataGenerationService("test-key");
            var aiTask = geminiService.IsAvailableAsync();
            aiTask.Wait(); // Wait for completion
            var aiAvailable = aiTask.Result;
            aiStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ AI Service Check: {aiStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 AI Available: {aiAvailable}");

            // Performance Summary
            _totalStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ===== PERFORMANCE SUMMARY =====");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Total Test Time: {_totalStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Breakdown:");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   SQL Parsing: {parsingStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   Constraint Extraction: {constraintStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   Bogus Generation: {bogusStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   Coordinated Generation: {coordinatedStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   AI Service Check: {aiStopwatch.Elapsed.TotalSeconds:F2}s");

            // Identify bottlenecks
            var timings = new Dictionary<string, double>
            {
                ["SQL Parsing"] = parsingStopwatch.Elapsed.TotalSeconds,
                ["Constraint Extraction"] = constraintStopwatch.Elapsed.TotalSeconds,
                ["Bogus Generation"] = bogusStopwatch.Elapsed.TotalSeconds,
                ["Coordinated Generation"] = coordinatedStopwatch.Elapsed.TotalSeconds,
                ["AI Service Check"] = aiStopwatch.Elapsed.TotalSeconds
            };

            var bottleneck = timings.OrderByDescending(kvp => kvp.Value).First();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🚨 BOTTLENECK: {bottleneck.Key} took {bottleneck.Value:F2}s");

            // Performance recommendations
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 💡 PERFORMANCE RECOMMENDATIONS:");
            if (bottleneck.Key == "Constraint Extraction")
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Cache constraint information");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Optimize constraint queries");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Reduce regex complexity");
            }
            else if (bottleneck.Key == "Coordinated Generation")
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Optimize data generation algorithms");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Reduce constraint validation overhead");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Cache generated data patterns");
            }
            else if (bottleneck.Key == "Bogus Generation")
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Optimize Bogus data generation");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Reduce string formatting overhead");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Cache common data patterns");
            }
            else if (bottleneck.Key == "AI Service Check")
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Cache AI service availability");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Reduce network calls");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Implement connection pooling");
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🏁 Performance Analysis Test Completed");
        }

        private static DatabaseInfo CreateMockDatabaseInfo()
        {
            var databaseInfo = new DatabaseInfo
            {
                Type = DatabaseType.Oracle,
                ConnectionString = "mock-connection"
            };

            // Add mock tables
            var usersTable = new TableSchema
            {
                TableName = "users",
                Columns = new List<ColumnSchema>
                {
                    new() { ColumnName = "id", DataType = "NUMBER", IsPrimaryKey = true, IsIdentity = true },
                    new() { ColumnName = "username", DataType = "VARCHAR2(50)", MaxLength = 50 },
                    new() { ColumnName = "first_name", DataType = "VARCHAR2(100)", MaxLength = 100 },
                    new() { ColumnName = "last_name", DataType = "VARCHAR2(100)", MaxLength = 100 },
                    new() { ColumnName = "email", DataType = "VARCHAR2(255)", MaxLength = 255 },
                    new() { ColumnName = "date_of_birth", DataType = "DATE" },
                    new() { ColumnName = "salary", DataType = "NUMBER(10,2)" },
                    new() { ColumnName = "department", DataType = "VARCHAR2(100)", MaxLength = 100 },
                    new() { ColumnName = "hire_date", DataType = "DATE" },
                    new() { ColumnName = "is_active", DataType = "NUMBER(1)" },
                    new() { ColumnName = "company_id", DataType = "NUMBER" },
                    new() { ColumnName = "created_at", DataType = "TIMESTAMP" }
                },
                ForeignKeys = new List<ForeignKeySchema>
                {
                    new() { ColumnName = "company_id", ReferencedTable = "companies", ReferencedColumn = "id" }
                }
            };

            var companiesTable = new TableSchema
            {
                TableName = "companies",
                Columns = new List<ColumnSchema>
                {
                    new() { ColumnName = "id", DataType = "NUMBER", IsPrimaryKey = true, IsIdentity = true },
                    new() { ColumnName = "NAME", DataType = "VARCHAR2(255)", MaxLength = 255 },
                    new() { ColumnName = "code", DataType = "VARCHAR2(50)", MaxLength = 50 }
                }
            };

            var rolesTable = new TableSchema
            {
                TableName = "roles",
                Columns = new List<ColumnSchema>
                {
                    new() { ColumnName = "id", DataType = "NUMBER", IsPrimaryKey = true, IsIdentity = true },
                    new() { ColumnName = "NAME", DataType = "VARCHAR2(255)", MaxLength = 255 },
                    new() { ColumnName = "code", DataType = "VARCHAR2(50)", MaxLength = 50 }
                }
            };

            var userRolesTable = new TableSchema
            {
                TableName = "user_roles",
                Columns = new List<ColumnSchema>
                {
                    new() { ColumnName = "id", DataType = "NUMBER", IsPrimaryKey = true, IsIdentity = true },
                    new() { ColumnName = "user_id", DataType = "NUMBER" },
                    new() { ColumnName = "role_id", DataType = "NUMBER" },
                    new() { ColumnName = "is_active", DataType = "NUMBER(1)" },
                    new() { ColumnName = "expires_at", DataType = "DATE" }
                },
                ForeignKeys = new List<ForeignKeySchema>
                {
                    new() { ColumnName = "user_id", ReferencedTable = "users", ReferencedColumn = "id" },
                    new() { ColumnName = "role_id", ReferencedTable = "roles", ReferencedColumn = "id" }
                }
            };

            databaseInfo.Tables["users"] = usersTable;
            databaseInfo.Tables["companies"] = companiesTable;
            databaseInfo.Tables["roles"] = rolesTable;
            databaseInfo.Tables["user_roles"] = userRolesTable;

            return databaseInfo;
        }
    }
} 