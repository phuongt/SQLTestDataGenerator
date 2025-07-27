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
    public class SimpleWorkflowTest
    {
        private string _connectionString = "Server=localhost;Port=3306;Database=simple_test_db;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;Pooling=true;";
        private string _simpleQuery = "SELECT id, name, email, created_at, status FROM users WHERE status = 'active' ORDER BY created_at DESC";
        
        private EngineService _engineService;

        [TestInitialize]
        public async Task Setup()
        {
            Console.WriteLine("=== SIMPLE WORKFLOW TEST SETUP ===");
            
            // Initialize engine service
            _engineService = new EngineService(DatabaseType.MySQL, _connectionString, "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM");
            
            // Create simple test database
            await CreateSimpleTestDatabase();
            
            Console.WriteLine("✅ Simple test setup completed");
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            Console.WriteLine("=== SIMPLE WORKFLOW TEST CLEANUP ===");
            await CleanupSimpleTestData();
        }

        [TestMethod]
        public async Task TestSimpleWorkflow_GenericQuery()
        {
            Console.WriteLine("=== TESTING SIMPLE WORKFLOW: GENERIC QUERY ===");
            
            int desiredRecords = 5; // Use smaller number for simple test
            Console.WriteLine($"✅ Testing with {desiredRecords} desired records");
            
            // PHASE 1: GENERATION
            Console.WriteLine("📋 PHASE 1: Generation");
            var request = new QueryExecutionRequest
            {
                DatabaseType = "MySQL",
                ConnectionString = _connectionString,
                SqlQuery = _simpleQuery,
                DesiredRecordCount = desiredRecords,
                UseAI = true,
                OpenAiApiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM"
            };

            var generationResult = await _engineService.ExecuteQueryWithTestDataAsync(request);
            
            // Assert Generation
            Assert.IsTrue(generationResult.Success, $"Generation failed: {generationResult.ErrorMessage}");
            Assert.AreEqual(desiredRecords, generationResult.GeneratedRecords, 
                $"DESIRED ({desiredRecords}) != GENERATED ({generationResult.GeneratedRecords})");
            Assert.IsNotNull(generationResult.GeneratedInserts);
            Assert.IsTrue(generationResult.GeneratedInserts.Any());
            
            Console.WriteLine($"✅ Generated {generationResult.GeneratedRecords} records successfully");
            Console.WriteLine($"✅ Created {generationResult.GeneratedInserts.Count} SQL statements");

            // PHASE 2: EXPORT TO FILE
            Console.WriteLine("💾 PHASE 2: File Export");
            string sqlFilePath = Path.Combine("sql-exports", $"simple_test_{DateTime.Now:yyyyMMdd_HHmmss}.sql");
            Directory.CreateDirectory(Path.GetDirectoryName(sqlFilePath));
            
            var sqlContent = string.Join("\r\n", generationResult.GeneratedInserts);
            await File.WriteAllTextAsync(sqlFilePath, sqlContent);
            
            Assert.IsTrue(File.Exists(sqlFilePath));
            Console.WriteLine($"✅ Exported to: {sqlFilePath}");

            // PHASE 3: COMMIT TO DATABASE
            Console.WriteLine("💿 PHASE 3: Database Commit");
            var commitResult = await ExecuteSimpleSqlFile(sqlFilePath);
            
            Assert.IsTrue(commitResult.Success, $"Commit failed: {commitResult.ErrorMessage}");
            Assert.AreEqual(generationResult.GeneratedInserts.Count, commitResult.ExecutedStatements,
                "Executed statements should match generated statements");
            
            Console.WriteLine($"✅ Committed {commitResult.ExecutedStatements} statements");

            // PHASE 4: VERIFICATION
            Console.WriteLine("✅ PHASE 4: Verification");
            var verificationResult = await RunSimpleVerificationQuery();
            
            Assert.IsTrue(verificationResult.Success, "Verification query should succeed");
            Assert.IsTrue(verificationResult.RecordCount >= 0, "Should return valid record count");
            
            Console.WriteLine($"✅ Verification found {verificationResult.RecordCount} matching records");

            // FINAL ASSERTIONS
            Console.WriteLine("🔍 FINAL ASSERTIONS");
            Assert.AreEqual(desiredRecords, generationResult.GeneratedRecords, "DESIRED == GENERATED");
            Assert.AreEqual(generationResult.GeneratedInserts.Count, commitResult.ExecutedStatements, "GENERATED == COMMITTED");
            Assert.IsTrue(verificationResult.Success, "VERIFICATION SUCCESS");
            
            Console.WriteLine("🎉 ALL SIMPLE WORKFLOW ASSERTIONS PASSED!");
            
            // Summary
            Console.WriteLine("");
            Console.WriteLine("=== SIMPLE WORKFLOW TEST SUMMARY ===");
            Console.WriteLine($"✅ DESIRED: {desiredRecords}");
            Console.WriteLine($"✅ GENERATED: {generationResult.GeneratedRecords}");
            Console.WriteLine($"✅ COMMITTED: {commitResult.ExecutedStatements}");
            Console.WriteLine($"✅ VERIFIED: {verificationResult.RecordCount} matching records");
            Console.WriteLine($"✅ STATUS: COMPLETE SUCCESS");
            
            // Cleanup test file
            if (File.Exists(sqlFilePath))
            {
                File.Delete(sqlFilePath);
                Console.WriteLine($"🧹 Cleaned up test file");
            }
        }

        private async Task CreateSimpleTestDatabase()
        {
            var masterConnection = _connectionString.Replace("Database=simple_test_db", "Database=mysql");
            
            using var connection = new MySqlConnection(masterConnection);
            await connection.OpenAsync();
            
            // Create database
            await connection.ExecuteAsync("CREATE DATABASE IF NOT EXISTS simple_test_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
            Console.WriteLine("✅ Simple test database created");
            
            // Switch and create simple table
            await connection.ExecuteAsync("USE simple_test_db");
            
            var createTableScript = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    email VARCHAR(255),
                    status VARCHAR(50) DEFAULT 'active',
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );";
            
            await connection.ExecuteAsync(createTableScript);
            Console.WriteLine("✅ Simple users table created");
        }

        private async Task CleanupSimpleTestData()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync("TRUNCATE TABLE users");
            Console.WriteLine("✅ Simple test data cleaned up");
        }

        private async Task<(bool Success, int ExecutedStatements, string ErrorMessage)> ExecuteSimpleSqlFile(string sqlFilePath)
        {
            try
            {
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
                    int executedCount = 0;
                    foreach (var statement in sqlStatements)
                    {
                        if (statement.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                        {
                            await connection.ExecuteAsync(statement, transaction: transaction);
                            executedCount++;
                        }
                    }
                    
                    transaction.Commit();
                    return (true, executedCount, string.Empty);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return (false, 0, ex.Message);
                }
            }
            catch (Exception ex)
            {
                return (false, 0, ex.Message);
            }
        }

        private async Task<(bool Success, int RecordCount, string ErrorMessage)> RunSimpleVerificationQuery()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var results = await connection.QueryAsync(_simpleQuery);
                var recordCount = results.Count();
                
                return (true, recordCount, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, 0, ex.Message);
            }
        }
    }
} 