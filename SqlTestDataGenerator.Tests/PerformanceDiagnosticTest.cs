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
    public class PerformanceDiagnosticTest
    {
        private string _connectionString = string.Empty;
        private EngineService? _engineService;
        private readonly Stopwatch _totalStopwatch = new Stopwatch();

        [TestInitialize]
        public async Task Setup()
        {
            _totalStopwatch.Start();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🚀 Starting Performance Diagnostic Test");
            
            // Initialize centralized logging
            CentralizedLoggingManager.Initialize();
            
            // Load configuration
            var configService = new ConfigurationService();
            var settings = configService.LoadUserSettings();
            var apiKey = settings?.OpenAiApiKey ?? "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM";
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔧 Using API Key: {apiKey.Substring(0, 10)}...");
            
            // Test Oracle connection
            _connectionString = "Data Source=localhost:1521/XE;User Id=system;Password=oracle;";
            
            try
            {
                using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(_connectionString);
                await connection.OpenAsync();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Oracle connection successful");
                _engineService = new EngineService(DatabaseType.Oracle, _connectionString, apiKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ Oracle connection failed: {ex.Message}");
                Assert.Inconclusive("Oracle database not available for performance testing");
            }
        }

        [TestMethod]
        public async Task Diagnostic_StepByStep_Performance_Analysis()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔍 Starting Step-by-Step Performance Analysis");
            
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

            var request = new QueryExecutionRequest
            {
                DatabaseType = "Oracle",
                ConnectionString = _connectionString,
                SqlQuery = complexSql,
                DesiredRecordCount = 5,
                OpenAiApiKey = null,
                UseAI = false, // Disable AI to test pure generation performance
                CurrentRecordCount = 0
            };

            // Step 1: Test Connection Performance
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔗 Step 1: Testing Connection Performance");
            var connectionStopwatch = Stopwatch.StartNew();
            var connectionOk = await _engineService!.TestConnectionAsync("Oracle", _connectionString);
            connectionStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Connection test: {connectionStopwatch.Elapsed.TotalSeconds:F2}s");

            // Step 2: Test SQL Parsing Performance
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📝 Step 2: Testing SQL Parsing Performance");
            var parsingStopwatch = Stopwatch.StartNew();
            var queryParser = new SqlQueryParserV3();
            var parsedQuery = queryParser.ParseQuery(complexSql);
            parsingStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ SQL Parsing: {parsingStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Parsed: {parsedQuery.TableRequirements.Count} tables, {parsedQuery.WhereConditions.Count} WHERE conditions");

            // Step 3: Test Schema Loading Performance
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📋 Step 3: Testing Schema Loading Performance");
            var schemaStopwatch = Stopwatch.StartNew();
            var metadataService = new SqlMetadataService();
            var tables = SqlMetadataService.ExtractTablesFromQuery(complexSql);
            var databaseInfo = await metadataService.GetDatabaseInfoAsync("Oracle", _connectionString, string.Join(" ", tables));
            schemaStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Schema Loading: {schemaStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Loaded: {databaseInfo.Tables.Count} tables");

            // Step 4: Test Constraint Extraction Performance
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔗 Step 4: Testing Constraint Extraction Performance");
            var constraintStopwatch = Stopwatch.StartNew();
            var constraintExtractor = new ComprehensiveConstraintExtractor();
            var constraints = constraintExtractor.ExtractAllConstraints(complexSql);
            constraintStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Constraint Extraction: {constraintStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Extracted: {constraints.LikePatterns.Count} LIKE patterns, {constraints.JoinConstraints.Count} JOIN constraints");

            // Step 5: Test Dependency Resolution Performance
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔄 Step 5: Testing Dependency Resolution Performance");
            var dependencyStopwatch = Stopwatch.StartNew();
            var dependencyResolver = new EnhancedDependencyResolver();
            var parsedQueryResult = dependencyResolver.ParseSelectQuery(complexSql, databaseInfo);
            var orderedTables = dependencyResolver.OrderTablesByDependencies(parsedQueryResult.AllRequiredTables, databaseInfo);
            dependencyStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Dependency Resolution: {dependencyStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Order: {string.Join(" → ", orderedTables)}");

            // Step 6: Test Data Generation Performance (Bottleneck Analysis)
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🤖 Step 6: Testing Data Generation Performance");
            var generationStopwatch = Stopwatch.StartNew();
            
            // Test different generation approaches
            var dataGenService = new DataGenService();
            
            // 6.1: Test Bogus Generation Performance
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔄 6.1: Testing Bogus Generation");
            var bogusStopwatch = Stopwatch.StartNew();
            var bogusStatements = dataGenService.GenerateBogusData(databaseInfo, 5, complexSql, constraints, null);
            bogusStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Bogus Generation: {bogusStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Generated: {bogusStatements.Count} statements");

            // 6.2: Test Coordinated Generation Performance
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🔄 6.2: Testing Coordinated Generation");
            var coordinatedStopwatch = Stopwatch.StartNew();
            var coordinatedGenerator = new CoordinatedDataGenerator();
            var coordinatedStatements = await coordinatedGenerator.GenerateCoordinatedDataAsync(
                databaseInfo, complexSql, 5, "Oracle", _connectionString);
            coordinatedStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Coordinated Generation: {coordinatedStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Generated: {coordinatedStatements.Count} statements");

            generationStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Total Data Generation: {generationStopwatch.Elapsed.TotalSeconds:F2}s");

            // Step 7: Test Full Execution Performance
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🚀 Step 7: Testing Full Execution Performance");
            var fullExecutionStopwatch = Stopwatch.StartNew();
            
            try
            {
                var result = await _engineService!.ExecuteQueryWithTestDataAsync(request);
                fullExecutionStopwatch.Stop();
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Full Execution: {fullExecutionStopwatch.Elapsed.TotalSeconds:F2}s");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Success: {result.Success}, Generated: {result.GeneratedRecords}");
                
                if (result.Success)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 📊 Result Rows: {result.ResultData.Rows.Count}");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ Error: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                fullExecutionStopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ Full Execution Failed: {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ⏱️ Failed after: {fullExecutionStopwatch.Elapsed.TotalSeconds:F2}s");
            }

            // Performance Summary
            _totalStopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ===== PERFORMANCE SUMMARY =====");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Total Test Time: {_totalStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Breakdown:");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   Connection: {connectionStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   SQL Parsing: {parsingStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   Schema Loading: {schemaStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   Constraint Extraction: {constraintStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   Dependency Resolution: {dependencyStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   Bogus Generation: {bogusStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   Coordinated Generation: {coordinatedStopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   Full Execution: {fullExecutionStopwatch.Elapsed.TotalSeconds:F2}s");

            // Identify bottlenecks
            var timings = new Dictionary<string, double>
            {
                ["Connection"] = connectionStopwatch.Elapsed.TotalSeconds,
                ["SQL Parsing"] = parsingStopwatch.Elapsed.TotalSeconds,
                ["Schema Loading"] = schemaStopwatch.Elapsed.TotalSeconds,
                ["Constraint Extraction"] = constraintStopwatch.Elapsed.TotalSeconds,
                ["Dependency Resolution"] = dependencyStopwatch.Elapsed.TotalSeconds,
                ["Bogus Generation"] = bogusStopwatch.Elapsed.TotalSeconds,
                ["Coordinated Generation"] = coordinatedStopwatch.Elapsed.TotalSeconds,
                ["Full Execution"] = fullExecutionStopwatch.Elapsed.TotalSeconds
            };

            var bottleneck = timings.OrderByDescending(kvp => kvp.Value).First();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🚨 BOTTLENECK: {bottleneck.Key} took {bottleneck.Value:F2}s");

            // Performance recommendations
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 💡 PERFORMANCE RECOMMENDATIONS:");
            if (bottleneck.Key == "Schema Loading")
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Cache database schema information");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Optimize metadata queries");
            }
            else if (bottleneck.Key == "Constraint Extraction")
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Cache constraint information");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Optimize constraint queries");
            }
            else if (bottleneck.Key == "Coordinated Generation")
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Optimize data generation algorithms");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Reduce constraint validation overhead");
            }
            else if (bottleneck.Key == "Full Execution")
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Check database execution performance");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   • Optimize INSERT statements");
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 🏁 Performance Diagnostic Test Completed");
        }
    }
} 