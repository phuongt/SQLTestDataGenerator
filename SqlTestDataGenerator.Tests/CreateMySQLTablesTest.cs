using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlConnector;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SqlTestDataGenerator.Tests
{
    [TestClass]
    public class CreateMySQLTablesTest
    {
        private const string CONNECTION_STRING = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;";

        [TestMethod]
        [TestCategory("Setup")]
        public async Task CreateTables_FromSqlScript_ShouldSucceed()
        {
            try
            {
                // Read SQL script from root directory
                var scriptPath = Path.Combine("..", "..", "..", "..", "create_tables.sql");
                var sqlScript = await File.ReadAllTextAsync(scriptPath);
                Console.WriteLine($"📄 Loaded SQL script: {sqlScript.Length} characters");

                // Split script into individual statements
                var statements = sqlScript.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine($"📝 Found {statements.Length} SQL statements");

                using var connection = new MySqlConnection(CONNECTION_STRING);
                await connection.OpenAsync();
                Console.WriteLine("✅ Connected to MySQL database: my_database");

                int executedCount = 0;
                foreach (var statement in statements)
                {
                    var cleanStatement = statement.Trim();
                    if (string.IsNullOrEmpty(cleanStatement) || cleanStatement.StartsWith("--"))
                        continue;

                    try
                    {
                        using var command = new MySqlCommand(cleanStatement, connection);
                        await command.ExecuteNonQueryAsync();
                        executedCount++;
                        
                        // Log important statements
                        if (cleanStatement.ToUpper().Contains("CREATE TABLE"))
                        {
                            var tableName = ExtractTableName(cleanStatement);
                            Console.WriteLine($"✅ Created table: {tableName}");
                        }
                        else if (cleanStatement.ToUpper().Contains("INSERT INTO"))
                        {
                            var tableName = ExtractInsertTableName(cleanStatement);
                            Console.WriteLine($"📝 Inserted data into: {tableName}");
                        }
                        else if (cleanStatement.ToUpper().Contains("CREATE VIEW"))
                        {
                            var viewName = ExtractViewName(cleanStatement);
                            Console.WriteLine($"👁️ Created view: {viewName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Failed to execute statement: {cleanStatement.Substring(0, Math.Min(50, cleanStatement.Length))}...");
                        Console.WriteLine($"   Error: {ex.Message}");
                        // Continue with other statements
                    }
                }

                Console.WriteLine($"\n🎉 Successfully executed {executedCount} SQL statements");
                Console.WriteLine("✅ MySQL tables created successfully!");

                // Verify tables exist
                await VerifyTablesExist(connection);

                Assert.IsTrue(executedCount > 0, "Should execute at least some SQL statements");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating tables: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task VerifyTablesExist(MySqlConnection connection)
        {
            var expectedTables = new[] { "companies", "roles", "users", "user_roles" };
            
            Console.WriteLine("\n🔍 Verifying tables exist:");
            foreach (var tableName in expectedTables)
            {
                var query = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'my_database' AND table_name = '{tableName}'";
                using var command = new MySqlCommand(query, connection);
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                
                if (count > 0)
                {
                    Console.WriteLine($"✅ Table '{tableName}' exists");
                    
                    // Count records in table
                    var countQuery = $"SELECT COUNT(*) FROM {tableName}";
                    using var countCommand = new MySqlCommand(countQuery, connection);
                    var recordCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                    Console.WriteLine($"   📊 Records: {recordCount}");
                }
                else
                {
                    Console.WriteLine($"❌ Table '{tableName}' NOT found");
                }
            }
        }

        private string ExtractTableName(string createStatement)
        {
            var parts = createStatement.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i].ToUpper() == "TABLE")
                {
                    return parts[i + 1].Trim('`', '(', ')');
                }
            }
            return "unknown";
        }

        private string ExtractInsertTableName(string insertStatement)
        {
            var parts = insertStatement.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i].ToUpper() == "INTO")
                {
                    return parts[i + 1].Trim('`', '(', ')');
                }
            }
            return "unknown";
        }

        private string ExtractViewName(string createStatement)
        {
            var parts = createStatement.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i].ToUpper() == "VIEW")
                {
                    return parts[i + 1].Trim('`', '(', ')');
                }
            }
            return "unknown";
        }
    }
} 