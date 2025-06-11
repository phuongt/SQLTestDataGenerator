using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace SqlTestDataGenerator.Tests;

[TestClass]
public class SqlQueryParserDebugTests
{
    [TestMethod]
    public void TestWhereClauseRegex()
    {
        // Test the WHERE clause regex pattern
        var wherePattern = @"\bWHERE\s+(.*?)(?:\s+(?:ORDER\s+BY|GROUP\s+BY|HAVING|LIMIT)|$)";
        var sql = "SELECT * FROM users WHERE status = 'active'";
        
        var match = Regex.Match(sql, wherePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        Assert.IsTrue(match.Success, "Should match WHERE clause");
        Assert.AreEqual("status = 'active'", match.Groups[1].Value);
    }

    [TestMethod]
    public void TestJoinRegexPattern()
    {
        // Test simpler JOIN regex patterns
        var sql = "SELECT u.name, c.name FROM users u INNER JOIN companies c ON u.company_id = c.id";
        
        // Pattern 1: Original complex pattern
        var complexPattern = @"(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+)?JOIN\s+(\w+)\s+(\w+)\s+ON\s+([^()]+?)(?=\s+(?:INNER|LEFT|RIGHT|FULL|WHERE|ORDER|GROUP|LIMIT|$))";
        var complexMatch = Regex.Match(sql, complexPattern, RegexOptions.IgnoreCase);
        
        // Pattern 2: Simplified pattern
        var simplePattern = @"(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+)?JOIN\s+(\w+)\s+(\w+)\s+ON\s+([^;]+?)(?:\s+(?:WHERE|ORDER|GROUP|LIMIT)|$)";
        var simpleMatch = Regex.Match(sql, simplePattern, RegexOptions.IgnoreCase);
        
        // Pattern 3: Even simpler
        var basicPattern = @"(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+)?JOIN\s+(\w+)\s+(\w+)\s+ON\s+(.+?)(?:\s+WHERE|\s+ORDER|\s+GROUP|\s+LIMIT|$)";
        var basicMatch = Regex.Match(sql, basicPattern, RegexOptions.IgnoreCase);
        
        System.Console.WriteLine($"SQL: {sql}");
        System.Console.WriteLine($"Complex pattern match: {complexMatch.Success}");
        System.Console.WriteLine($"Simple pattern match: {simpleMatch.Success}");
        System.Console.WriteLine($"Basic pattern match: {basicMatch.Success}");
        
        if (basicMatch.Success)
        {
            System.Console.WriteLine($"Table: {basicMatch.Groups[1].Value}");
            System.Console.WriteLine($"Alias: {basicMatch.Groups[2].Value}");
            System.Console.WriteLine($"ON condition: {basicMatch.Groups[3].Value}");
        }
        
        Assert.IsTrue(basicMatch.Success, "Basic pattern should match JOIN clause");
        if (basicMatch.Success)
        {
            Assert.AreEqual("companies", basicMatch.Groups[1].Value, "Should extract table name");
            Assert.AreEqual("c", basicMatch.Groups[2].Value, "Should extract table alias");
            Assert.AreEqual("u.company_id = c.id", basicMatch.Groups[3].Value.Trim(), "Should extract ON condition");
        }
    }

    [TestMethod]
    public void TestDifferentSQLFormats()
    {
        var testCases = new[]
        {
            "SELECT * FROM users WHERE status = 'active'",
            "SELECT * FROM users WHERE age = 25",
            "SELECT * FROM users WHERE is_active = TRUE",
            "SELECT * FROM users u WHERE u.status = 'active'",
            "SELECT * FROM users WHERE name LIKE '%John%'"
        };

        // Updated pattern to match SqlQueryParser fix
        var wherePattern = @"\bWHERE\s+(.*?)(?:\s+(?:ORDER\s+BY|GROUP\s+BY|HAVING|LIMIT)|$)";

        foreach (var sql in testCases)
        {
            Console.WriteLine($"\n=== Testing: {sql} ===");
            
            var whereMatch = Regex.Match(sql, wherePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Console.WriteLine($"WHERE Match: {whereMatch.Success}");
            
            if (whereMatch.Success)
            {
                Console.WriteLine($"WHERE Clause: '{whereMatch.Groups[1].Value}'");
            }
        }
    }
} 