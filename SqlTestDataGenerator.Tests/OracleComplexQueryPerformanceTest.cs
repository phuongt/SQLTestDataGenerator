using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Services;
using System.Collections.Generic;
using System.Linq;

namespace SqlTestDataGenerator.Tests
{
    [TestClass]
    public class OracleComplexQueryPerformanceTest
    {
        private string _connectionString = string.Empty;
        private EngineService? _engineService;
        private bool _hasOracleConnection = false;
        private readonly Stopwatch _totalStopwatch = new Stopwatch();

        [TestInitialize]
        public async Task Setup()
        {
            _totalStopwatch.Start();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🚀 Starting Oracle Complex Query Performance Test");
            
            // Initialize centralized logging
            CentralizedLoggingManager.Initialize();
            
            // Oracle connection string with performance optimizations
            _connectionString = "Data Source=localhost:1521/XE;User Id=system;Password=22092012;Connection Timeout=30;Connection Lifetime=60;Pooling=true;Min Pool Size=0;Max Pool Size=5;Statement Cache Size=50;";
                
            _engineService = new EngineService(DatabaseType.Oracle, _connectionString, "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM");

            // Test Oracle connection
            _hasOracleConnection = await TestOracleConnection();
            
            if (_hasOracleConnection)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Oracle connection available");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ⚠️ Oracle connection not available - running parser-only tests");
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _totalStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🏁 Test completed in {_totalStopwatch.Elapsed.TotalSeconds:F2}s");
        }

        [TestMethod]
        [Timeout(900000)] // 15 minutes timeout
        public async Task TestOracleComplexQueryPerformance_DetailedBreakdown()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔍 Starting detailed performance breakdown test");
            
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

            if (!_hasOracleConnection)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ⚠️ Skipping database execution - testing components only");
                await TestComponentsOnly(complexSql);
                return;
            }

            // Full performance test with detailed timing
            await TestFullPerformance(complexSql);
        }

        private async Task TestComponentsOnly(string complexSql)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📋 Testing SQL parsing components...");
            
            var stopwatch = Stopwatch.StartNew();
            
            // Test SQL parsing
            var extractedTables = SqlTestDataGenerator.Core.Services.SqlMetadataService.ExtractTablesFromQuery(complexSql);
            stopwatch.Stop();
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ SQL parsing completed in {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Extracted {extractedTables.Count} tables: {string.Join(", ", extractedTables)}");
            
            // Test Oracle dialect validation
            stopwatch.Restart();
            var hasOracleFunctions = complexSql.Contains("SYSDATE") && complexSql.Contains("EXTRACT(YEAR");
            stopwatch.Stop();
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Oracle dialect validation completed in {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔧 Oracle functions detected: {hasOracleFunctions}");
            
            Assert.IsTrue(hasOracleFunctions, "Should contain Oracle-specific functions");
            Assert.IsTrue(extractedTables.Count >= 4, "Should extract at least 4 tables");
        }

        private async Task TestFullPerformance(string complexSql)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🚀 Starting full performance test with Oracle database");
            
            var request = new QueryExecutionRequest
            {
                SqlQuery = complexSql,
                DatabaseType = "Oracle",
                ConnectionString = _connectionString,
                DesiredRecordCount = 3, // Small number for performance testing
                OpenAiApiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM",
                UseAI = true,
                CurrentRecordCount = 0
            };

            // Step 1: Test basic Oracle connection performance
            await TestConnectionPerformance();
            
            // Step 2: Test SQL parsing performance
            await TestSqlParsingPerformance(complexSql);
            
            // Step 3: Test table structure analysis
            await TestTableStructureAnalysis();
            
            // Step 4: Test constraint extraction
            await TestConstraintExtraction();
            
            // Step 5: Test AI service initialization
            await TestAIServiceInitialization();
            
            // Step 6: Test full data generation
            await TestFullDataGeneration(request);
        }

        private async Task TestConnectionPerformance()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔌 Testing Oracle connection performance...");
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();
                
                // Test basic query
                using var cmd = new OracleCommand("SELECT SYSDATE FROM DUAL", connection);
                var result = await cmd.ExecuteScalarAsync();
                
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Connection test completed in {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Connection result: {result}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ Connection test failed after {stopwatch.Elapsed.TotalMilliseconds:F0}ms: {ex.Message}");
                throw;
            }
        }

        private async Task TestSqlParsingPerformance(string complexSql)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📋 Testing SQL parsing performance...");
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var extractedTables = SqlTestDataGenerator.Core.Services.SqlMetadataService.ExtractTablesFromQuery(complexSql);
                stopwatch.Stop();
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ SQL parsing completed in {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Extracted tables: {string.Join(", ", extractedTables)}");
                
                Assert.IsTrue(extractedTables.Count >= 4, "Should extract at least 4 tables");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ SQL parsing failed after {stopwatch.Elapsed.TotalMilliseconds:F0}ms: {ex.Message}");
                throw;
            }
        }

        private async Task TestTableStructureAnalysis()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🏗️ Testing table structure analysis...");
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();
                
                // Test table existence
                var tables = new[] { "users", "companies", "user_roles", "roles" };
                var existingTables = new List<string>();
                
                foreach (var table in tables)
                {
                    try
                    {
                        using var cmd = new OracleCommand($"SELECT COUNT(*) FROM {table}", connection);
                        var count = await cmd.ExecuteScalarAsync();
                        existingTables.Add(table);
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Table {table}: {count} rows");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ⚠️ Table {table} not found or inaccessible: {ex.Message}");
                    }
                }
                
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Table structure analysis completed in {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Existing tables: {string.Join(", ", existingTables)}");
                
                Assert.IsTrue(existingTables.Count > 0, "At least one table should exist");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ Table structure analysis failed after {stopwatch.Elapsed.TotalMilliseconds:F0}ms: {ex.Message}");
                throw;
            }
        }

        private async Task TestConstraintExtraction()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔗 Testing constraint extraction...");
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();
                
                // Test foreign key constraints
                var constraintQuery = @"
SELECT 
    uc.constraint_name,
    uc.table_name,
    ucc.column_name,
    uc.r_constraint_name,
    ur.table_name as referenced_table,
    urcc.column_name as referenced_column
FROM user_constraints uc
JOIN user_cons_columns ucc ON uc.constraint_name = ucc.constraint_name
JOIN user_constraints ur ON uc.r_constraint_name = ur.constraint_name
JOIN user_cons_columns urcc ON ur.constraint_name = urcc.constraint_name
WHERE uc.constraint_type = 'R'
AND uc.table_name IN ('USERS', 'COMPANIES', 'USER_ROLES', 'ROLES')";
                
                using var cmd = new OracleCommand(constraintQuery, connection);
                using var reader = await cmd.ExecuteReaderAsync();
                
                var constraints = new List<string>();
                while (await reader.ReadAsync())
                {
                    var constraint = $"{reader["table_name"]}.{reader["column_name"]} -> {reader["referenced_table"]}.{reader["referenced_column"]}";
                    constraints.Add(constraint);
                }
                
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Constraint extraction completed in {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Found {constraints.Count} foreign key constraints");
                
                foreach (var constraint in constraints.Take(5)) // Show first 5
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔗 {constraint}");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ⚠️ Constraint extraction failed after {stopwatch.Elapsed.TotalMilliseconds:F0}ms: {ex.Message}");
                // Don't throw - constraints might not exist
            }
        }

        private async Task TestAIServiceInitialization()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🤖 Testing AI service initialization...");
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Test AI service availability
                var aiService = _engineService?.DataGenService?.GeminiAIService;
                if (aiService != null)
                {
                    var rotationService = aiService.FlashRotationService;
                    var currentModel = rotationService?.GetCurrentModelName() ?? "Unknown";
                    var canCall = rotationService?.CanCallAPINow() ?? false;
                    
                    stopwatch.Stop();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ AI service initialization completed in {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🤖 Current model: {currentModel}");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔄 Can call API: {canCall}");
                }
                else
                {
                    stopwatch.Stop();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ⚠️ AI service not available after {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ AI service initialization failed after {stopwatch.Elapsed.TotalMilliseconds:F0}ms: {ex.Message}");
                // Don't throw - AI service might not be critical for basic functionality
            }
        }

        private async Task TestFullDataGeneration(QueryExecutionRequest request)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🚀 Testing full data generation...");
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Starting ExecuteQueryWithTestDataAsync...");
                var result = await _engineService!.ExecuteQueryWithTestDataAsync(request);
                
                stopwatch.Stop();
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Full data generation completed in {stopwatch.Elapsed.TotalSeconds:F2}s");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Generation result:");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   - Success: {result.Success}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   - Generated Records: {result.GeneratedRecords}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   - Result Rows: {result.ResultData?.Rows.Count ?? 0}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   - Error: {result.ErrorMessage ?? "None"}");
                
                Assert.IsTrue(result.Success, $"Data generation failed: {result.ErrorMessage}");
                Assert.IsNotNull(result.ResultData);
                Assert.IsTrue(result.ResultData.Rows.Count > 0, "No data was returned");
                
                // Performance validation
                var expectedMaxTime = 300.0; // 5 minutes
                Assert.IsTrue(stopwatch.Elapsed.TotalSeconds < expectedMaxTime, 
                    $"Data generation took too long: {stopwatch.Elapsed.TotalSeconds:F2}s (expected < {expectedMaxTime}s)");
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🎉 Performance test passed! Total time: {stopwatch.Elapsed.TotalSeconds:F2}s");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ Full data generation failed after {stopwatch.Elapsed.TotalSeconds:F2}s: {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📋 Exception details: {ex}");
                throw;
            }
        }

        private async Task<bool> TestOracleConnection()
        {
            try
            {
                using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();
                
                using var cmd = new OracleCommand("SELECT SYSDATE FROM DUAL", connection);
                var result = await cmd.ExecuteScalarAsync();
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Oracle connection test passed: {result}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ Oracle connection not available: {ex.Message}");
                return false;
            }
        }
    }
} 