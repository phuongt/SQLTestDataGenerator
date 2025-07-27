using System;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;

class Program
{
    static void Main()
    {
        var builder = new CommonInsertBuilder(new ConsoleLogger());
        
        Console.WriteLine("Testing datetime string detection and formatting:");
        
        var testValues = new[]
        {
            "1989-08-09",
            "2025-03-03 09:41:00",
            "2025-07-04 15:34:55",
            "1989-12-20",
            "test string"
        };
        
        foreach (var value in testValues)
        {
            try
            {
                var result = builder.FormatValue(value, DatabaseType.MySQL);
                Console.WriteLine($"Input: '{value}' -> Output: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Input: '{value}' -> ERROR: {ex.Message}");
            }
        }
    }
}

public class ConsoleLogger : ILogger
{
    public void Debug(string template, params object[] args) => Console.WriteLine($"[DEBUG] {string.Format(template, args)}");
    public void Information(string template, params object[] args) => Console.WriteLine($"[INFO] {string.Format(template, args)}");
    public void Warning(string template, params object[] args) => Console.WriteLine($"[WARN] {string.Format(template, args)}");
    public void Error(string template, params object[] args) => Console.WriteLine($"[ERROR] {string.Format(template, args)}");
    public void Error(Exception ex, string template, params object[] args) => Console.WriteLine($"[ERROR] {string.Format(template, args)} - {ex.Message}");
} 