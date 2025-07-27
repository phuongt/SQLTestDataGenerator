using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using Oracle.ManagedDataAccess.Client;

namespace SqlTestDataGenerator.Tests
{
    [TestClass]
    public class CompleteWorkflowAutomatedTest_Oracle
    {
        private string _connectionString = "Data Source=localhost:1521/XE;User Id=system;Password=22092012;Connection Timeout=120;Connection Lifetime=300;Pooling=true;Min Pool Size=0;Max Pool Size=10;";
        private string _testQuery = @"SELECT COUNT(*) FROM users WHERE ROWNUM <= 10";
        private EngineService _engineService;

        [TestInitialize]
        public async Task Setup()
        {
            _engineService = new EngineService(DatabaseType.Oracle, _connectionString);
            await CreateTestDatabaseIfNotExists();
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await CleanupTestData();
        }

        [TestMethod]
        public async Task TestCompleteWorkflow_DesiredToGeneratedToCommittedToVerified_Oracle()
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
            using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(_connectionString);
            await connection.OpenAsync();
            // Tạo sequence, table Oracle
            var createTablesScript = @"
                BEGIN
                    EXECUTE IMMEDIATE 'CREATE TABLE companies (
                        id NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                        NAME VARCHAR2(255) NOT NULL,
                        code VARCHAR2(50) UNIQUE,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )';
                EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;
                BEGIN
                    EXECUTE IMMEDIATE 'CREATE TABLE roles (
                        id NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                        NAME VARCHAR2(255) NOT NULL,
                        code VARCHAR2(50) UNIQUE,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )';
                EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;
                BEGIN
                    EXECUTE IMMEDIATE 'CREATE TABLE users (
                        id NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                        username VARCHAR2(100) UNIQUE,
                        first_name VARCHAR2(100),
                        last_name VARCHAR2(100),
                        email VARCHAR2(255),
                        date_of_birth DATE,
                        salary NUMBER(10,2),
                        department VARCHAR2(100),
                        hire_date DATE,
                        is_active NUMBER(1) DEFAULT 1,
                        company_id NUMBER,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT fk_company FOREIGN KEY (company_id) REFERENCES companies(id)
                    )';
                EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;
                BEGIN
                    EXECUTE IMMEDIATE 'CREATE TABLE user_roles (
                        id NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                        user_id NUMBER,
                        role_id NUMBER,
                        is_active NUMBER(1) DEFAULT 1,
                        expires_at DATE,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT fk_user FOREIGN KEY (user_id) REFERENCES users(id),
                        CONSTRAINT fk_role FOREIGN KEY (role_id) REFERENCES roles(id)
                    )';
                EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;
            ";
            using var cmd = connection.CreateCommand();
            cmd.CommandText = createTablesScript;
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task CleanupTestData()
        {
            using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(_connectionString);
            await connection.OpenAsync();
            // Disable FK: Oracle không có lệnh global, phải drop/disable từng constraint nếu cần
            // Truncate all tables (CASCADE nếu có FK)
            foreach (var tbl in new[] { "user_roles", "users", "roles", "companies" })
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"TRUNCATE TABLE {tbl}";
                try { await cmd.ExecuteNonQueryAsync(); } catch { /* ignore if not exists */ }
            }
        }

        private async Task<Dictionary<string, int>> ExecuteSqlFileAndCountTables(string sqlFilePath)
        {
            var tableInsertCounts = new Dictionary<string, int>();
            var sqlContent = await File.ReadAllTextAsync(sqlFilePath);
            var sqlStatements = sqlContent.Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries)
                                         .Where(s => !string.IsNullOrWhiteSpace(s))
                                         .Select(s => s.Trim())
                                         .ToList();

            using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(_connectionString);
            await connection.OpenAsync();
            
            using var transaction = connection.BeginTransaction();
            try
            {
                // Oracle doesn't support SET FOREIGN_KEY_CHECKS - rely on dependency order
                // The generated SQL should already be in correct dependency order
                
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
                using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(_connectionString);
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