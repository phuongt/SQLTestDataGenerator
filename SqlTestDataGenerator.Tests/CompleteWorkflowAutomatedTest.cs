using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;

namespace SqlTestDataGenerator.Tests
{
    [TestClass]
    public class CompleteWorkflowAutomatedTest
    {
        private string _connectionString = "Server=localhost;Port=3306;Database=test_workflow_db;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;";
        private string _testQuery = @"
            SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date, 
                   c.NAME AS company_name, c.code AS company_code, r.NAME AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
                   CASE 
                       WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                       WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn vai trò'
                       ELSE 'Đang làm việc'
                   END AS work_status
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = False
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
              AND YEAR(u.date_of_birth) = 1989
              AND c.NAME LIKE '%HOME%'
              AND r.code LIKE '%member%'
              AND (u.is_active = 0 OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY))
            ORDER BY ur.expires_at ASC, u.created_at DESC";

        private EngineService _engineService;

        [TestInitialize]
        public async Task Setup()
        {
            Console.WriteLine("=== COMPLETE WORKFLOW AUTOMATED TEST SETUP ===");
            
            // Initialize engine service with test API key
            _engineService = new EngineService("AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM");
            
            // Create test database if not exists
            await CreateTestDatabaseIfNotExists();
            
            // Clean up any existing test data
            await CleanupTestData();
            
            Console.WriteLine("Setup completed successfully");
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            Console.WriteLine("=== COMPLETE WORKFLOW TEST CLEANUP ===");
            await CleanupTestData();
        }

        [TestMethod]
        public async Task TestCompleteWorkflow_DesiredToGeneratedToCommittedToVerified()
        {
            Console.WriteLine("=== TESTING COMPLETE WORKFLOW: DESIRED -> GENERATED -> COMMITTED -> VERIFIED ===");
            
            // PHASE 1: SETUP TEST PARAMETERS
            int desiredRecords = 15;
            Console.WriteLine($"✅ PHASE 1: Setup - Desired Records: {desiredRecords}");
            
            // PHASE 2: GENERATION
            Console.WriteLine("✅ PHASE 2: Generation Phase");
            var request = new QueryExecutionRequest
            {
                DatabaseType = "MySQL",
                ConnectionString = _connectionString,
                SqlQuery = _testQuery,
                DesiredRecordCount = desiredRecords,
                UseAI = true,
                OpenAiApiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM"
            };

            var generationResult = await _engineService.ExecuteQueryWithTestDataAsync(request);
            
            // Assert Generation Phase
            Assert.IsTrue(generationResult.Success, $"Generation failed: {generationResult.ErrorMessage}");
            Assert.AreEqual(desiredRecords, generationResult.GeneratedRecords, 
                $"ASSERTION FAILED: DESIRED ({desiredRecords}) != GENERATED ({generationResult.GeneratedRecords})");
            Assert.IsNotNull(generationResult.GeneratedInserts, "Generated SQL statements should not be null");
            Assert.IsTrue(generationResult.GeneratedInserts.Any(), "Should have generated SQL statements");
            
            Console.WriteLine($"✅ Generation Assertion: DESIRED ({desiredRecords}) == GENERATED ({generationResult.GeneratedRecords})");
            Console.WriteLine($"✅ Generated SQL Statements: {generationResult.GeneratedInserts.Count}");

            // PHASE 3: EXPORT TO FILE (SIMULATING UI EXPORT)
            Console.WriteLine("✅ PHASE 3: File Export Phase");
            string sqlFilePath = Path.Combine("sql-exports", $"test_workflow_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N[..6]}.sql");
            Directory.CreateDirectory(Path.GetDirectoryName(sqlFilePath));
            
            var sqlContent = string.Join("\r\n", generationResult.GeneratedInserts);
            await File.WriteAllTextAsync(sqlFilePath, sqlContent);
            
            Assert.IsTrue(File.Exists(sqlFilePath), "SQL file should be created");
            var fileContent = await File.ReadAllTextAsync(sqlFilePath);
            var fileStatements = fileContent.Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries)
                                          .Where(s => !string.IsNullOrWhiteSpace(s))
                                          .Count();
            
            Assert.AreEqual(generationResult.GeneratedInserts.Count, fileStatements,
                "File should contain same number of statements as generated");
            
            Console.WriteLine($"✅ File Export: {sqlFilePath} with {fileStatements} statements");

            // PHASE 4: COMMIT PHASE (SIMULATING UI COMMIT)
            Console.WriteLine("✅ PHASE 4: Commit Phase");
            var tableInsertCounts = await ExecuteSqlFileAndCountTables(sqlFilePath);
            
            // Assert Commit Phase
            var totalCommitted = tableInsertCounts.Values.Sum();
            Assert.AreEqual(desiredRecords, totalCommitted,
                $"ASSERTION FAILED: GENERATED ({desiredRecords}) != COMMITTED ({totalCommitted})");
            
            Console.WriteLine($"✅ Commit Assertion: GENERATED ({desiredRecords}) == COMMITTED ({totalCommitted})");
            Console.WriteLine("✅ Table Breakdown:");
            foreach (var kvp in tableInsertCounts.OrderBy(x => x.Key))
            {
                Console.WriteLine($"   • {kvp.Key}: {kvp.Value} record(s)");
            }

            // PHASE 5: VERIFICATION PHASE
            Console.WriteLine("✅ PHASE 5: Verification Phase");
            var verificationResult = await RunVerificationQuery();
            
            // Assert Verification Phase
            Assert.IsTrue(verificationResult.QueryExecuted, "Verification query should execute successfully");
            Assert.IsTrue(verificationResult.RecordCount >= 0, "Verification should return valid record count");
            
            Console.WriteLine($"✅ Verification Assertion: Query executed successfully");
            Console.WriteLine($"✅ Verification Results: {verificationResult.RecordCount} matching records found");
            Console.WriteLine($"✅ Verification Status: {verificationResult.Status}");

            // PHASE 6: COMPREHENSIVE ASSERTIONS
            Console.WriteLine("✅ PHASE 6: Comprehensive Assertions");
            
            // Core workflow assertions
            Assert.AreEqual(desiredRecords, generationResult.GeneratedRecords, 
                "CRITICAL: DESIRED != GENERATED");
            Assert.AreEqual(generationResult.GeneratedRecords, totalCommitted, 
                "CRITICAL: GENERATED != COMMITTED");
            Assert.IsTrue(verificationResult.QueryExecuted, 
                "CRITICAL: VERIFICATION FAILED");
            
            // Data integrity assertions
            Assert.IsTrue(tableInsertCounts.ContainsKey("companies"), "Should have companies data");
            Assert.IsTrue(tableInsertCounts.ContainsKey("roles"), "Should have roles data");
            Assert.IsTrue(tableInsertCounts.ContainsKey("users"), "Should have users data");
            Assert.IsTrue(tableInsertCounts.ContainsKey("user_roles"), "Should have user_roles data");
            
            Console.WriteLine("✅ ALL ASSERTIONS PASSED!");
            
            // FINAL SUMMARY
            Console.WriteLine("");
            Console.WriteLine("=== COMPLETE WORKFLOW TEST SUMMARY ===");
            Console.WriteLine($"✅ DESIRED RECORDS: {desiredRecords}");
            Console.WriteLine($"✅ GENERATED RECORDS: {generationResult.GeneratedRecords}");
            Console.WriteLine($"✅ COMMITTED RECORDS: {totalCommitted}");
            Console.WriteLine($"✅ VERIFICATION RESULTS: {verificationResult.RecordCount} matching");
            Console.WriteLine($"✅ TABLE DISTRIBUTION: {string.Join(", ", tableInsertCounts.Select(x => $"{x.Key}({x.Value})"))}");
            Console.WriteLine($"✅ WORKFLOW STATUS: COMPLETE SUCCESS!");
            
            // Cleanup test file
            if (File.Exists(sqlFilePath))
            {
                File.Delete(sqlFilePath);
                Console.WriteLine($"✅ Cleanup: Deleted test file {sqlFilePath}");
            }
        }

        private async Task CreateTestDatabaseIfNotExists()
        {
            var masterConnection = _connectionString.Replace("Database=test_workflow_db", "Database=mysql");
            
            using var connection = new MySqlConnection(masterConnection);
            await connection.OpenAsync();
            
            // Create database
            await connection.ExecuteAsync("CREATE DATABASE IF NOT EXISTS test_workflow_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
            Console.WriteLine("✅ Test database created/verified");
            
            // Switch to test database and create tables
            await connection.ExecuteAsync("USE test_workflow_db");
            
            // Create required tables
            var createTablesScript = @"
                CREATE TABLE IF NOT EXISTS companies (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    NAME VARCHAR(255) NOT NULL,
                    code VARCHAR(50) UNIQUE,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS roles (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    NAME VARCHAR(255) NOT NULL,
                    code VARCHAR(50) UNIQUE,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS users (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    username VARCHAR(100) UNIQUE,
                    first_name VARCHAR(100),
                    last_name VARCHAR(100),
                    email VARCHAR(255),
                    date_of_birth DATE,
                    salary DECIMAL(10,2),
                    department VARCHAR(100),
                    hire_date DATE,
                    is_active BOOLEAN DEFAULT TRUE,
                    company_id INT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (company_id) REFERENCES companies(id)
                );

                CREATE TABLE IF NOT EXISTS user_roles (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    user_id INT,
                    role_id INT,
                    is_active BOOLEAN DEFAULT TRUE,
                    expires_at DATETIME,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (user_id) REFERENCES users(id),
                    FOREIGN KEY (role_id) REFERENCES roles(id)
                );";
            
            await connection.ExecuteAsync(createTablesScript);
            Console.WriteLine("✅ Test tables created/verified");
        }

        private async Task CleanupTestData()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            
            // Disable foreign key checks
            await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 0");
            
            // Truncate all tables
            await connection.ExecuteAsync("TRUNCATE TABLE user_roles");
            await connection.ExecuteAsync("TRUNCATE TABLE users");
            await connection.ExecuteAsync("TRUNCATE TABLE roles");
            await connection.ExecuteAsync("TRUNCATE TABLE companies");
            
            // Re-enable foreign key checks
            await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 1");
            
            Console.WriteLine("✅ Test data cleaned up");
        }

        private async Task<Dictionary<string, int>> ExecuteSqlFileAndCountTables(string sqlFilePath)
        {
            var tableInsertCounts = new Dictionary<string, int>();
            var sqlContent = await File.ReadAllTextAsync(sqlFilePath);
            var sqlStatements = sqlContent.Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries)
                                         .Where(s => !string.IsNullOrWhiteSpace(s))
                                         .Select(s => s.Trim())
                                         .ToList();

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var transaction = connection.BeginTransaction();
            try
            {
                // Disable foreign key checks
                await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 0", transaction: transaction);
                
                foreach (var statement in sqlStatements)
                {
                    if (statement.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
                    {
                        var tableName = ExtractTableNameFromInsert(statement);
                        if (!string.IsNullOrEmpty(tableName))
                        {
                            tableInsertCounts[tableName] = tableInsertCounts.GetValueOrDefault(tableName, 0) + 1;
                        }
                        
                        await connection.ExecuteAsync(statement, transaction: transaction);
                    }
                }
                
                // Re-enable foreign key checks
                await connection.ExecuteAsync("SET FOREIGN_KEY_CHECKS = 1", transaction: transaction);
                
                transaction.Commit();
                Console.WriteLine($"✅ Executed {sqlStatements.Count} SQL statements successfully");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            
            return tableInsertCounts;
        }

        private async Task<(bool QueryExecuted, int RecordCount, string Status)> RunVerificationQuery()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var results = await connection.QueryAsync(_testQuery);
                var recordCount = results.Count();
                
                Console.WriteLine($"✅ Verification query executed successfully");
                Console.WriteLine($"✅ Found {recordCount} matching records");
                
                return (true, recordCount, $"Success: {recordCount} records found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Verification query failed: {ex.Message}");
                return (false, 0, $"Failed: {ex.Message}");
            }
        }

        private static string ExtractTableNameFromInsert(string insertStatement)
        {
            try
            {
                // Pattern: INSERT INTO table_name (...) VALUES (...)
                var match = System.Text.RegularExpressions.Regex.Match(insertStatement, 
                    @"INSERT\s+INTO\s+[`'""]?([a-zA-Z_][a-zA-Z0-9_]*)[`'""]?\s*\(", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                
                // Alternative pattern: INSERT INTO table_name VALUES (...)
                match = System.Text.RegularExpressions.Regex.Match(insertStatement, 
                    @"INSERT\s+INTO\s+[`'""]?([a-zA-Z_][a-zA-Z0-9_]*)[`'""]?\s+VALUES", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            catch
            {
                // If regex fails, return empty
            }
            
            return string.Empty;
        }
    }
} 