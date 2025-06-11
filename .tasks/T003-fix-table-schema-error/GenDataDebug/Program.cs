using System;
using System.Threading.Tasks;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using Serilog;

namespace GenDataDebugTest
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== GenData Debug Test ===");
            Console.WriteLine("Analyzing step-by-step execution to identify and fix issues\n");
            
            var connectionString = "Server=sql.freedb.tech;Port=3306;Database=freedb_DBTest;Uid=freedb_TestAdmin;Pwd=Vt5B&Mx6Jcu#jeN;";
            var apiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM";
            
            // Test 1: Simple SQL
            await TestGenData("SELECT * FROM users", "Test 1: Simple SQL", connectionString, apiKey, 3);
            
            Console.WriteLine("\n" + new string('=', 80) + "\n");
            
            // Test 2: Complex SQL (problematic one)
            var complexSql = @"SELECT u.id, u.username, u.first_name, u.last_name, u.email,
       u.date_of_birth, u.salary, u.department, u.hire_date,
       c.NAME AS company_name, c.code AS company_code,
       r.NAME AS role_name, r.code AS role_code,
       ur.expires_at AS role_expires,
       CASE 
           WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
           WHEN ur.expires_at IS NOT NULL
               AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY)
               THEN 'Sắp hết hạn vai trò'
           ELSE 'Đang làm việc'
       END AS work_status
FROM users u
INNER JOIN companies c ON u.company_id = c.id  
INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = TRUE
INNER JOIN roles r ON ur.role_id = r.id
WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')";
            
            await TestGenData(complexSql, "Test 2: Complex JOIN SQL", connectionString, apiKey, 5);
            
            Console.WriteLine("\n" + new string('=', 80) + "\n");
            Console.WriteLine("=== Analysis Complete ===");
            Console.WriteLine("Check logs for detailed step-by-step execution analysis.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        
        private static async Task TestGenData(string sqlQuery, string testName, string connectionString, string apiKey, int recordCount)
        {
            Console.WriteLine($"=== {testName} ===");
            Console.WriteLine($"Query: {sqlQuery.Substring(0, Math.Min(100, sqlQuery.Length))}...");
            Console.WriteLine($"Target Records: {recordCount}\n");
            
            try
            {
                // Step 1: Create EngineService
                Console.WriteLine("STEP 1: Creating EngineService...");
                var engineService = new EngineService(apiKey);
                Console.WriteLine("✅ EngineService created\n");
                
                // Step 2: Test Connection
                Console.WriteLine("STEP 2: Testing database connection...");
                var connectionOk = await engineService.TestConnectionAsync("MySQL", connectionString);
                Console.WriteLine($"✅ Connection result: {connectionOk}\n");
                
                if (!connectionOk)
                {
                    Console.WriteLine("❌ Connection failed - stopping test");
                    return;
                }
                
                // Step 3: Create Request
                Console.WriteLine("STEP 3: Creating QueryExecutionRequest...");
                var request = new QueryExecutionRequest
                {
                    DatabaseType = "MySQL",
                    ConnectionString = connectionString,
                    SqlQuery = sqlQuery,
                    DesiredRecordCount = recordCount,
                    OpenAiApiKey = apiKey,
                    UseAI = true,
                    CurrentRecordCount = 0
                };
                Console.WriteLine("✅ Request created\n");
                
                // Step 4: Execute GenData - DETAILED LOGGING
                Console.WriteLine("STEP 4: Executing GenData with detailed logging...");
                Console.WriteLine("--- Starting ExecuteQueryWithTestDataAsync ---");
                
                // Manual step-by-step execution to catch exact failure point
                var result = await ExecuteWithDetailedLogging(engineService, request);
                
                Console.WriteLine("--- GenData Execution Complete ---\n");
                
                // Step 5: Analyze Results
                Console.WriteLine("STEP 5: Analyzing results...");
                Console.WriteLine($"✅ Success: {result.Success}");
                Console.WriteLine($"✅ Generated Records: {result.GeneratedRecords}");
                Console.WriteLine($"✅ Execution Time: {result.ExecutionTime.TotalSeconds:F2}s");
                
                if (!result.Success)
                {
                    Console.WriteLine($"❌ Error: {result.ErrorMessage}");
                }
                
                if (result.ResultData != null)
                {
                    Console.WriteLine($"✅ Result Rows: {result.ResultData.Rows.Count}");
                }
                
                if (result.GeneratedInserts != null && result.GeneratedInserts.Count > 0)
                {
                    Console.WriteLine($"✅ Generated INSERT statements: {result.GeneratedInserts.Count}");
                    Console.WriteLine("First INSERT statement:");
                    Console.WriteLine($"   {result.GeneratedInserts[0]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ EXCEPTION in {testName}:");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
                Console.WriteLine($"   Stack: {ex.StackTrace}");
            }
        }
        
        private static async Task<QueryExecutionResult> ExecuteWithDetailedLogging(EngineService engineService, QueryExecutionRequest request)
        {
            Console.WriteLine("🔍 DETAILED EXECUTION LOGGING:");
            Console.WriteLine($"   Database Type: {request.DatabaseType}");
            Console.WriteLine($"   Query Length: {request.SqlQuery.Length} chars");
            Console.WriteLine($"   Desired Records: {request.DesiredRecordCount}");
            Console.WriteLine($"   Use AI: {request.UseAI}");
            Console.WriteLine($"   Has API Key: {!string.IsNullOrEmpty(request.OpenAiApiKey)}");
            
            Console.WriteLine("\n🔍 CALLING ExecuteQueryWithTestDataAsync...");
            
            try
            {
                var result = await engineService.ExecuteQueryWithTestDataAsync(request);
                Console.WriteLine("🔍 ExecuteQueryWithTestDataAsync returned successfully");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔍 ExecuteQueryWithTestDataAsync threw exception: {ex.GetType().Name}");
                Console.WriteLine($"🔍 Exception message: {ex.Message}");
                
                // Return error result
                return new QueryExecutionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = TimeSpan.Zero
                };
            }
        }
    }
} 