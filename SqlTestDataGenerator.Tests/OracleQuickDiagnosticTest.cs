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
    public class OracleQuickDiagnosticTest
    {
        private string _connectionString = string.Empty;
        private EngineService? _engineService;

        [TestInitialize]
        public async Task Setup()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🚀 Starting Oracle Quick Diagnostic Test");
            
            // Initialize centralized logging
            CentralizedLoggingManager.Initialize();
            
            // Oracle connection string
            _connectionString = "Data Source=localhost:1521/XE;User Id=system;Password=22092012;Connection Timeout=30;Connection Lifetime=60;Pooling=true;Min Pool Size=0;Max Pool Size=5;";
                
            _engineService = new EngineService(DatabaseType.Oracle, _connectionString, "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM");
        }

        [TestMethod]
        [Timeout(300000)] // 5 minutes timeout
        public async Task TestOracleQuickDiagnostic_StepByStep()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔍 Starting step-by-step diagnostic");
            
            // Step 1: Test basic connection
            await TestBasicConnection();
            
            // Step 2: Test SQL parsing only
            await TestSqlParsingOnly();
            
            // Step 3: Test constraint extraction separately
            await TestConstraintExtractionSeparately();
            
            // Step 4: Test AI service status
            await TestAIServiceStatus();
            
            // Step 5: Test data generation without AI
            await TestDataGenerationWithoutAI();
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ All diagnostic steps completed");
        }

        private async Task TestBasicConnection()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔌 Step 1: Testing basic Oracle connection...");
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();
                
                using var cmd = new OracleCommand("SELECT SYSDATE FROM DUAL", connection);
                var result = await cmd.ExecuteScalarAsync();
                
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Connection test: {stopwatch.Elapsed.TotalMilliseconds:F0}ms - {result}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ Connection failed: {stopwatch.Elapsed.TotalMilliseconds:F0}ms - {ex.Message}");
                throw;
            }
        }

        private async Task TestSqlParsingOnly()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📋 Step 2: Testing SQL parsing only...");
            
            var complexSql = @"
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

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var extractedTables = SqlTestDataGenerator.Core.Services.SqlMetadataService.ExtractTablesFromQuery(complexSql);
                stopwatch.Stop();
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ SQL parsing: {stopwatch.Elapsed.TotalMilliseconds:F0}ms - {extractedTables.Count} tables");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Tables: {string.Join(", ", extractedTables)}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ SQL parsing failed: {stopwatch.Elapsed.TotalMilliseconds:F0}ms - {ex.Message}");
                throw;
            }
        }

        private async Task TestConstraintExtractionSeparately()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔗 Step 3: Testing constraint extraction separately...");
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();
                
                // Simplified constraint query
                var constraintQuery = @"
SELECT COUNT(*) as constraint_count
FROM user_constraints uc
WHERE uc.constraint_type = 'R'
AND uc.table_name IN ('USERS', 'COMPANIES', 'USER_ROLES', 'ROLES')";
                
                using var cmd = new OracleCommand(constraintQuery, connection);
                var result = await cmd.ExecuteScalarAsync();
                
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Constraint count: {stopwatch.Elapsed.TotalMilliseconds:F0}ms - {result} constraints");
                
                // If constraints exist, test detailed extraction
                if (Convert.ToInt32(result) > 0)
                {
                    await TestDetailedConstraintExtraction(connection);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ Constraint extraction failed: {stopwatch.Elapsed.TotalMilliseconds:F0}ms - {ex.Message}");
                // Don't throw - constraints might not be critical
            }
        }

        private async Task TestDetailedConstraintExtraction(OracleConnection connection)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔗 Testing detailed constraint extraction...");
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var detailedQuery = @"
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
                
                using var cmd = new OracleCommand(detailedQuery, connection);
                using var reader = await cmd.ExecuteReaderAsync();
                
                var constraints = new List<string>();
                while (await reader.ReadAsync())
                {
                    var constraint = $"{reader["table_name"]}.{reader["column_name"]} -> {reader["referenced_table"]}.{reader["referenced_column"]}";
                    constraints.Add(constraint);
                }
                
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Detailed constraints: {stopwatch.Elapsed.TotalMilliseconds:F0}ms - {constraints.Count} found");
                
                foreach (var constraint in constraints.Take(3)) // Show first 3
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔗 {constraint}");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ⚠️ Detailed constraint extraction failed: {stopwatch.Elapsed.TotalMilliseconds:F0}ms - {ex.Message}");
            }
        }

        private async Task TestAIServiceStatus()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🤖 Step 4: Testing AI service status...");
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var aiService = _engineService?.DataGenService?.GeminiAIService;
                if (aiService != null)
                {
                    var rotationService = aiService.FlashRotationService;
                    var currentModel = rotationService?.GetCurrentModelName() ?? "Unknown";
                    var canCall = rotationService?.CanCallAPINow() ?? false;
                    var apiStats = rotationService?.GetAPIUsageStatistics();
                    
                    stopwatch.Stop();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ AI service: {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🤖 Current model: {currentModel}");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔄 Can call API: {canCall}");
                    
                    if (apiStats != null)
                    {
                        var dailyUsed = apiStats.ContainsKey("DailyCallsUsed") ? apiStats["DailyCallsUsed"] : 0;
                        var dailyLimit = apiStats.ContainsKey("DailyCallLimit") ? apiStats["DailyCallLimit"] : 100;
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 API usage: {dailyUsed}/{dailyLimit}");
                    }
                }
                else
                {
                    stopwatch.Stop();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ⚠️ AI service not available: {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ AI service test failed: {stopwatch.Elapsed.TotalMilliseconds:F0}ms - {ex.Message}");
            }
        }

        private async Task TestDataGenerationWithoutAI()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🚀 Step 5: Testing data generation WITHOUT AI...");
            
            var complexSql = @"
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

            var request = new QueryExecutionRequest
            {
                SqlQuery = complexSql,
                DatabaseType = "Oracle",
                ConnectionString = _connectionString,
                DesiredRecordCount = 2, // Small number for testing
                OpenAiApiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM",
                UseAI = false, // DISABLE AI for this test
                CurrentRecordCount = 0
            };

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Starting ExecuteQueryWithTestDataAsync (AI disabled)...");
                var result = await _engineService!.ExecuteQueryWithTestDataAsync(request);
                
                stopwatch.Stop();
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Data generation (no AI): {stopwatch.Elapsed.TotalSeconds:F2}s");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Result: Success={result.Success}, Generated={result.GeneratedRecords}, Rows={result.ResultData?.Rows.Count ?? 0}");
                
                if (!result.Success)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ Error: {result.ErrorMessage}");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🎉 Data generation without AI completed successfully!");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ Data generation failed: {stopwatch.Elapsed.TotalSeconds:F2}s - {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📋 Exception details: {ex}");
                throw;
            }
        }
    }
} 