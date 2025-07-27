using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using System.Threading.Tasks;

namespace SqlTestDataGenerator.Tests;

[TestClass]
public class DatabaseConnectionTest
{
    private EngineService _engineService = null!;

    [TestInitialize]
    public void Setup()
    {
        _engineService = new EngineService(DatabaseType.MySQL, "Server=localhost;Port=3306;Database=test;Uid=root;Pwd=;CharSet=utf8mb4;");
    }

    [TestMethod]
    [TestCategory("ConnectionTest")]
    [Timeout(180000)] // 3 minutes timeout
    public async Task TestLocalMySQL_ShouldConnect()
    {
        // Test local MySQL (XAMPP, WAMP, etc.)
        var localConnections = new[]
        {
            "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;",
            "Server=localhost;Port=3306;Database=test;Uid=root;Pwd=;CharSet=utf8mb4;",
            "Server=localhost;Port=3306;Database=test;Uid=root;Pwd=root;CharSet=utf8mb4;",
            "Server=127.0.0.1;Port=3306;Database=test;Uid=root;Pwd=;CharSet=utf8mb4;",
            "Server=127.0.0.1;Port=3306;Database=mysql;Uid=root;Pwd=;CharSet=utf8mb4;"
        };

        Console.WriteLine("=== TESTING LOCAL MYSQL CONNECTIONS ===");
        bool foundConnection = false;
        string workingConnection = "";
        
        foreach (var connectionString in localConnections)
        {
            Console.WriteLine($"\nTesting: {connectionString}");
            try
            {
                var connected = await _engineService.TestConnectionAsync("MySQL", connectionString);
                Console.WriteLine($"Result: {(connected ? "✅ SUCCESS" : "❌ FAILED")}");
                
                if (connected)
                {
                    Console.WriteLine($"🎉 FOUND WORKING LOCAL MYSQL: {connectionString}");
                    foundConnection = true;
                    workingConnection = connectionString;
                    break; // Found working connection
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
        
        if (!foundConnection)
        {
            Console.WriteLine("❌ No local MySQL connection found");
        }
        else
        {
            Console.WriteLine($"✅ Working MySQL connection found: {workingConnection}");
        }
        
        Assert.IsTrue(foundConnection, "Should find at least one working local MySQL connection");
    }



    [TestMethod]
    [TestCategory("ConnectionTest")]
    [Timeout(120000)] // 2 minutes timeout
    public async Task TestCloudMySQL_AlternativeProviders()
    {
        // Test các cloud MySQL providers khác
        var cloudConnections = new[]
        {
            // PlanetScale
            "Server=aws.connect.psdb.cloud;Database=testdb;Uid=testuser;Pwd=pscale_pw_testpass;SslMode=Required;",
            
            // Railway
            "Server=containers-us-west-1.railway.app;Port=3306;Database=railway;Uid=root;Pwd=testpass;",
            
            // Clever Cloud
            "Server=bmvz4eqz1wprlm2o.cbetxkdyhwsb.us-east-1.rds.amazonaws.com;Port=3306;Database=testdb;Uid=testuser;Pwd=testpass;",
            
            // Aiven
            "Server=mysql-test.aivencloud.com;Port=12345;Database=defaultdb;Uid=avnadmin;Pwd=testpass;SslMode=Required;"
        };

        Console.WriteLine("=== TESTING CLOUD MYSQL PROVIDERS ===");
        Console.WriteLine("(These are example formats - real credentials needed)");
        
        foreach (var connectionString in cloudConnections)
        {
            Console.WriteLine($"\nConnection format: {connectionString.Split(';')[0]}...");
            Console.WriteLine("❓ Need real credentials to test");
        }
        
        Console.WriteLine("\n📝 If you have cloud MySQL, please provide connection string!");
    }
} 