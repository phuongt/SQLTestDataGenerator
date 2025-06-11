using MySqlConnector;
using System;
using System.Threading.Tasks;

namespace MySqlVowisQueryTests;

public class QuickConnectionTest
{
    public static async Task Main(string[] args)
    {
        var connectionString = "Server=mysql.freedb.tech;Port=3306;Database=freedb_DBTest;Uid=freedb_Tuan2024;Pwd=Qjmzqb37sV6#fZy;";
        
        Console.WriteLine("=== Quick MySQL Connection Test ===");
        Console.WriteLine($"Testing connection to: {connectionString.Substring(0, 50)}...");
        
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            
            Console.WriteLine("✅ Connection successful!");
            
            // Test basic query
            using var cmd = new MySqlCommand("SELECT DATABASE(), USER(), NOW()", connection);
            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                Console.WriteLine($"Database: {reader.GetString(0)}");
                Console.WriteLine($"User: {reader.GetString(1)}");
                Console.WriteLine($"Server Time: {reader.GetDateTime(2)}");
            }
            
            // Check if required tables exist
            reader.Close();
            using var tablesCmd = new MySqlCommand(@"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = DATABASE() 
                  AND table_name IN ('users', 'companies', 'roles', 'user_roles')
                ORDER BY table_name", connection);
            
            using var tablesReader = await tablesCmd.ExecuteReaderAsync();
            Console.WriteLine("\nRequired tables:");
            while (await tablesReader.ReadAsync())
            {
                Console.WriteLine($"  ✅ {tablesReader.GetString(0)}");
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Connection failed: {ex.Message}");
            Console.WriteLine($"Full error: {ex}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
} 