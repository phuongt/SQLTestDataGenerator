using System;
using System.IO;
using System.Threading.Tasks;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;

namespace UnitTest
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Unit Test - Generate Data với SELECT * FROM users ===");
            
            // 1. Test ExtractTablesFromQuery trước
            Console.WriteLine("\n1. Testing ExtractTablesFromQuery...");
            var sqlQuery = "SELECT * FROM users";
            Console.WriteLine($"SQL Query: {sqlQuery}");
            
            try
            {
                var tables = SqlMetadataService.ExtractTablesFromQuery(sqlQuery);
                Console.WriteLine($"✅ Tables extracted: {tables.Count}");
                foreach (var table in tables)
                {
                    Console.WriteLine($"  - {table}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ExtractTablesFromQuery failed: {ex.Message}");
                Console.WriteLine($"Exception: {ex}");
            }
            
            Console.WriteLine("\n" + new string('=', 60));
            
            // 2. Test với connection thật
            Console.WriteLine("\n2. Testing Generate Data với MySQL connection...");
            
            var connectionString = "Server=sql.freedb.tech;Port=3306;Database=freedb_DBTest;Uid=freedb_TestAdmin;Pwd=Vt5B&Mx6Jcu#jeN;";
            var apiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM";
            
            try
            {
                Console.WriteLine("Creating EngineService...");
                var engineService = new EngineService(apiKey);
                
                Console.WriteLine("Testing connection...");
                var connectionOk = await engineService.TestConnectionAsync("MySQL", connectionString);
                Console.WriteLine($"Connection test result: {connectionOk}");
                
                if (connectionOk)
                {
                    Console.WriteLine("\nTesting Generate Data...");
                    var request = new QueryExecutionRequest
                    {
                        DatabaseType = "MySQL",
                        ConnectionString = connectionString,
                        SqlQuery = sqlQuery,
                        DesiredRecordCount = 5,
                        OpenAiApiKey = apiKey,
                        UseAI = true,
                        CurrentRecordCount = 0
                    };
                    
                    Console.WriteLine("Calling ExecuteQueryWithTestDataAsync...");
                    var result = await engineService.ExecuteQueryWithTestDataAsync(request);
                    
                    Console.WriteLine($"\n=== RESULT ===");
                    Console.WriteLine($"Success: {result.Success}");
                    Console.WriteLine($"Generated Records: {result.GeneratedRecords}");
                    Console.WriteLine($"Error Message: {result.ErrorMessage}");
                    Console.WriteLine($"Execution Time: {result.ExecutionTime.TotalSeconds:F2}s");
                    
                    if (result.ResultData != null)
                    {
                        Console.WriteLine($"Result Rows: {result.ResultData.Rows.Count}");
                    }
                    
                    if (result.GeneratedInserts != null && result.GeneratedInserts.Count > 0)
                    {
                        Console.WriteLine($"\nGenerated INSERT statements ({result.GeneratedInserts.Count}):");
                        for (int i = 0; i < Math.Min(3, result.GeneratedInserts.Count); i++)
                        {
                            Console.WriteLine($"  {i+1}. {result.GeneratedInserts[i]}");
                        }
                        if (result.GeneratedInserts.Count > 3)
                        {
                            Console.WriteLine($"  ... and {result.GeneratedInserts.Count - 3} more");
                        }
                    }
                    
                    if (result.Success)
                    {
                        Console.WriteLine("\n🎉 Generate Data SUCCESS!");
                    }
                    else
                    {
                        Console.WriteLine($"\n❌ Generate Data FAILED: {result.ErrorMessage}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Connection failed, skipping Generate Data test");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
            
            Console.WriteLine("\n" + new string('=', 60));
            
            // 3. Check logs
            Console.WriteLine("\n3. Checking log files...");
            var currentDir = Directory.GetCurrentDirectory();
            Console.WriteLine($"Current directory: {currentDir}");
            
            var logsDir = Path.Combine(currentDir, "logs");
            Console.WriteLine($"Logs directory: {logsDir}");
            Console.WriteLine($"Logs directory exists: {Directory.Exists(logsDir)}");
            
            if (Directory.Exists(logsDir))
            {
                var logFiles = Directory.GetFiles(logsDir, "*.txt");
                Console.WriteLine($"Found {logFiles.Length} log files:");
                foreach (var file in logFiles)
                {
                    var info = new FileInfo(file);
                    Console.WriteLine($"  - {Path.GetFileName(file)} ({info.Length} bytes, {info.LastWriteTime})");
                    
                    // Show last few lines
                    try
                    {
                        var lines = File.ReadAllLines(file);
                        if (lines.Length > 0)
                        {
                            Console.WriteLine("    Last few lines:");
                            for (int i = Math.Max(0, lines.Length - 5); i < lines.Length; i++)
                            {
                                Console.WriteLine($"      {lines[i]}");
                            }
                        }
                    }
                    catch (Exception logEx)
                    {
                        Console.WriteLine($"    Error reading log: {logEx.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("Logs directory does not exist. Logs may not be created yet or path issue.");
                
                // Try different possible paths
                var alternativePaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SqlTestDataGenerator", "logs"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SqlTestDataGenerator", "logs"),
                    Path.Combine(currentDir, "..", "..", "logs"),
                    "logs"
                };
                
                Console.WriteLine("Checking alternative log paths:");
                foreach (var path in alternativePaths)
                {
                    Console.WriteLine($"  - {path}: {Directory.Exists(path)}");
                    if (Directory.Exists(path))
                    {
                        var files = Directory.GetFiles(path, "*.txt");
                        Console.WriteLine($"    Found {files.Length} files");
                    }
                }
            }
            
            Console.WriteLine("\n=== Unit Test Complete ===");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
} 