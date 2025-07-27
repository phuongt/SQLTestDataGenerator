using System;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Abstractions;

class Program
{
    static void Main()
    {
        Console.WriteLine("🔧 Debug Oracle Date Formatting");
        
        // Test the problematic date values from the error
        var testValues = new[]
        {
            "1976-12-31 00:41:47",
            "2025-04-11 02:05:09",
            "2025-07-24 21:37:59",
            "2025-04-18 07:24:43",
            "2025-07-21 20:47:30"
        };
        
        var dialectHandler = new OracleDialectHandler();
        var insertBuilder = new CommonInsertBuilder(dialectHandler);
        
        Console.WriteLine("\n=== Testing FormatUnknownValue ===");
        foreach (var value in testValues)
        {
            var result = insertBuilder.FormatValue(value, DatabaseType.Oracle);
            Console.WriteLine($"Input: '{value}' -> Output: {result}");
        }
        
        Console.WriteLine("\n=== Testing FormatDateTime ===");
        foreach (var value in testValues)
        {
            if (DateTime.TryParse(value, out var dt))
            {
                var result = insertBuilder.FormatValue(dt, DatabaseType.Oracle);
                Console.WriteLine($"Input: {dt} -> Output: {result}");
            }
        }
        
        Console.WriteLine("\n=== Testing IsLikelyDateTimeString ===");
        foreach (var value in testValues)
        {
            var isDateTime = IsLikelyDateTimeString(value);
            Console.WriteLine($"'{value}' -> IsDateTime: {isDateTime}");
        }
    }
    
    // Copy the method from CommonInsertBuilder for testing
    private static bool IsLikelyDateTimeString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        // Common datetime patterns: YYYY-MM-DD or YYYY-MM-DD HH:MM:SS
        return value.Contains("-") && 
               (value.Contains(":") || value.Length == 10) && 
               (value.Length >= 10 && value.Length <= 19) &&
               char.IsDigit(value[0]) &&
               DateTime.TryParse(value, out _);
    }
} 