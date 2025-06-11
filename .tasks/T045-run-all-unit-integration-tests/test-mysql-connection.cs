using System;
using MySqlConnector;
using System.Threading.Tasks;

class TestMySQLConnection 
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔍 Testing MySQL Connection...");
        
        // Test các connection strings khác nhau
        var connectionStrings = new[]
        {
            "Server=localhost;Database=testdb;Uid=root;Pwd=password;Connection Timeout=120;",
            "Server=localhost;Database=testdb;Uid=root;Pwd=;Connection Timeout=120;",
            "Server=localhost;Database=mysql;Uid=root;Pwd=password;Connection Timeout=120;",
            "Server=localhost;Database=mysql;Uid=root;Pwd=;Connection Timeout=120;",
            "Server=127.0.0.1;Port=3306;Database=mysql;Uid=root;Pwd=password;Connection Timeout=120;",
            "Server=127.0.0.1;Port=3306;Database=mysql;Uid=root;Pwd=;Connection Timeout=120;"
        };

        foreach (var connStr in connectionStrings)
        {
            await TestConnection(connStr);
        }
    }

    static async Task TestConnection(string connectionString)
    {
        try
        {
            Console.WriteLine($"\n🧪 Testing: {connectionString.Replace("Pwd=password", "Pwd=***")}");
            
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            
            var cmd = new MySqlCommand("SELECT VERSION()", connection);
            var version = await cmd.ExecuteScalarAsync();
            
            Console.WriteLine($"✅ SUCCESS! MySQL Version: {version}");
            
            // Test show databases
            cmd = new MySqlCommand("SHOW DATABASES", connection);
            using var reader = await cmd.ExecuteReaderAsync();
            Console.WriteLine("📁 Available databases:");
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"   - {reader.GetString(0)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ FAILED: {ex.Message}");
        }
    }
} 