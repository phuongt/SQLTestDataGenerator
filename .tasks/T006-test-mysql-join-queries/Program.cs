using System;
using System.Threading.Tasks;
using MySqlJoinTests;

namespace MySqlJoinTestRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 MySQL JOIN Queries Test Runner");
            Console.WriteLine("=================================");
            Console.WriteLine();

            try
            {
                var testSuite = new MySqlJoinTest();
                await testSuite.RunAllTests();

                Console.WriteLine("\n✅ All tests completed!");
                Console.WriteLine("Check logs/mysql-join-test-*.txt for detailed information.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test runner failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
} 