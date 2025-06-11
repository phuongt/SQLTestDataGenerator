using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlTestDataGenerator.Tests;

[TestClass]
public class SqlQueryParserTests
{
    private SqlQueryParser _parser;

    [TestInitialize]
    public void Setup()
    {
        _parser = new SqlQueryParser();
    }

    [TestMethod]
    public void ParseQuery_SimpleWhereEquality_ShouldExtractCondition()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE status = 'active'";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count, "Should extract one WHERE condition");
        var condition = result.WhereConditions.First();
        Assert.AreEqual("status", condition.ColumnName);
        Assert.AreEqual("=", condition.Operator);
        Assert.AreEqual("active", condition.Value);
    }

    [TestMethod]
    public void ParseQuery_WhereEqualityWithTableAlias_ShouldExtractCondition()
    {
        // Arrange
        var sql = "SELECT * FROM users u WHERE u.status = 'active'";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count);
        var condition = result.WhereConditions.First();
        Assert.AreEqual("u", condition.TableAlias);
        Assert.AreEqual("status", condition.ColumnName);
        Assert.AreEqual("=", condition.Operator);
        Assert.AreEqual("active", condition.Value);
    }

    [TestMethod]
    public void ParseQuery_WhereEqualityWithNumbers_ShouldExtractCondition()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE age = 25";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count);
        var condition = result.WhereConditions.First();
        Assert.AreEqual("age", condition.ColumnName);
        Assert.AreEqual("=", condition.Operator);
        Assert.AreEqual("25", condition.Value);
    }

    [TestMethod]
    public void ParseQuery_WhereEqualityWithBoolean_ShouldExtractCondition()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE is_active = TRUE";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count);
        var condition = result.WhereConditions.First();
        Assert.AreEqual("is_active", condition.ColumnName);
        Assert.AreEqual("=", condition.Operator);
        Assert.AreEqual("TRUE", condition.Value);
    }

    [TestMethod]
    public void ParseQuery_ComparisonOperators_ShouldExtractConditions()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE age >= 18 AND salary < 100000";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.AreEqual(2, result.WhereConditions.Count, "Should extract two WHERE conditions");
        
        var ageCondition = result.WhereConditions.FirstOrDefault(c => c.ColumnName == "age");
        Assert.IsNotNull(ageCondition);
        Assert.AreEqual(">=", ageCondition.Operator);
        Assert.AreEqual("18", ageCondition.Value);

        var salaryCondition = result.WhereConditions.FirstOrDefault(c => c.ColumnName == "salary");
        Assert.IsNotNull(salaryCondition);
        Assert.AreEqual("<", salaryCondition.Operator);
        Assert.AreEqual("100000", salaryCondition.Value);
    }

    [TestMethod]
    public void ParseQuery_LikeCondition_ShouldExtractCondition()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE name LIKE '%John%'";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count);
        var condition = result.WhereConditions.First();
        Assert.AreEqual("name", condition.ColumnName);
        Assert.AreEqual("LIKE", condition.Operator);
        Assert.AreEqual("John", condition.Value); // Should strip % wildcards
    }

    [TestMethod]
    public void ParseQuery_InCondition_ShouldExtractCondition()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE status IN ('active', 'pending', 'verified')";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count);
        var condition = result.WhereConditions.First();
        Assert.AreEqual("status", condition.ColumnName);
        Assert.AreEqual("IN", condition.Operator);
        Assert.AreEqual("active,pending,verified", condition.Value);
    }

    [TestMethod]
    public void ParseQuery_JoinConditions_ShouldExtractJoinRequirements()
    {
        // Arrange
        var sql = @"SELECT u.name, c.name 
                   FROM users u 
                   INNER JOIN companies c ON u.company_id = c.id";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.AreEqual(1, result.JoinRequirements.Count);
        var join = result.JoinRequirements.First();
        Assert.AreEqual("u", join.LeftTableAlias);
        Assert.AreEqual("company_id", join.LeftColumn);
        Assert.AreEqual("c", join.RightTableAlias);
        Assert.AreEqual("id", join.RightColumn);
        Assert.AreEqual("companies", join.RightTable);
    }

    [TestMethod]
    public void ParseQuery_ComplexQuery_ShouldHandleAllConditionTypes()
    {
        // Arrange
        var sql = @"SELECT u.name, c.name 
                   FROM users u 
                   INNER JOIN companies c ON u.company_id = c.id 
                   WHERE u.age >= 18 
                   AND u.status = 'active' 
                   AND u.name LIKE '%John%'
                   AND u.department IN ('IT', 'Sales')";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.IsTrue(result.WhereConditions.Count >= 4, "Should extract multiple WHERE conditions");
        Assert.AreEqual(1, result.JoinRequirements.Count, "Should extract JOIN requirement");
        
        // Verify specific conditions exist
        Assert.IsTrue(result.WhereConditions.Any(c => c.ColumnName == "age" && c.Operator == ">="));
        Assert.IsTrue(result.WhereConditions.Any(c => c.ColumnName == "status" && c.Operator == "="));
        Assert.IsTrue(result.WhereConditions.Any(c => c.ColumnName == "name" && c.Operator == "LIKE"));
        Assert.IsTrue(result.WhereConditions.Any(c => c.ColumnName == "department" && c.Operator == "IN"));
    }

    [TestMethod]
    public void ParseQuery_YearFunction_ShouldExtractDateCondition()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE YEAR(date_of_birth) = 1989";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count);
        var condition = result.WhereConditions.First();
        Assert.AreEqual("date_of_birth", condition.ColumnName);
        Assert.AreEqual("YEAR_EQUALS", condition.Operator);
        Assert.AreEqual("1989", condition.Value);
    }

    [TestMethod]
    public void TestJoinRegexPattern()
    {
        // Test JOIN regex pattern bị lỗi
        var joinPattern = @"(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+)?JOIN\s+(\w+)\s+(\w+)\s+ON\s+([^()]+?)(?=\s+(?:INNER|LEFT|RIGHT|FULL|WHERE|ORDER|GROUP|LIMIT|$))";
        var sql = @"SELECT u.name, c.name 
                   FROM users u 
                   INNER JOIN companies c ON u.company_id = c.id";
        
        var cleanSql = Regex.Replace(sql, @"\s+", " ").Trim();
        Console.WriteLine($"Clean SQL: '{cleanSql}'");
        
        var matches = Regex.Matches(cleanSql, joinPattern, RegexOptions.IgnoreCase);
        Console.WriteLine($"JOIN matches found: {matches.Count}");
        
        foreach (Match match in matches)
        {
            Console.WriteLine($"Full match: '{match.Value}'");
            Console.WriteLine($"Table: '{match.Groups[1].Value}'");
            Console.WriteLine($"Alias: '{match.Groups[2].Value}'");
            Console.WriteLine($"ON clause: '{match.Groups[3].Value}'");
        }
        
        // Test JOIN condition pattern
        var joinConditionPattern = @"(\w+)\.(\w+)\s*=\s*(\w+)\.(\w+)";
        var onClause = "u.company_id = c.id";
        var joinMatch = Regex.Match(onClause, joinConditionPattern);
        
        Console.WriteLine($"JOIN condition match success: {joinMatch.Success}");
        if (joinMatch.Success)
        {
            Console.WriteLine($"Alias1: '{joinMatch.Groups[1].Value}'");
            Console.WriteLine($"Column1: '{joinMatch.Groups[2].Value}'");
            Console.WriteLine($"Alias2: '{joinMatch.Groups[3].Value}'");
            Console.WriteLine($"Column2: '{joinMatch.Groups[4].Value}'");
        }
        
        // Debug why the complex pattern doesn't work
        Console.WriteLine($"SQL: {cleanSql}");
        
        // Test different patterns
        var complexPattern = @"(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+)?JOIN\s+(\w+)\s+(\w+)\s+ON\s+([^()]+?)(?=\s+(?:INNER|LEFT|RIGHT|FULL|WHERE|ORDER|GROUP|LIMIT|$))";
        var simplePattern = @"(?:INNER\s+)?JOIN\s+(\w+)\s+(\w+)\s+ON\s+([^()]+?)$";
        var basicPattern = @"JOIN\s+(\w+)\s+(\w+)\s+ON\s+(.+)$";
        
        Console.WriteLine($"Complex pattern match: {Regex.IsMatch(cleanSql, complexPattern, RegexOptions.IgnoreCase)}");
        Console.WriteLine($"Simple pattern match: {Regex.IsMatch(cleanSql, simplePattern, RegexOptions.IgnoreCase)}");
        Console.WriteLine($"Basic pattern match: {Regex.IsMatch(cleanSql, basicPattern, RegexOptions.IgnoreCase)}");
        
        // Use the basic pattern to get the match
        var basicMatch = Regex.Match(cleanSql, basicPattern, RegexOptions.IgnoreCase);
        if (basicMatch.Success)
        {
            Console.WriteLine($"Table: {basicMatch.Groups[1].Value}");
            Console.WriteLine($"Alias: {basicMatch.Groups[2].Value}");
            Console.WriteLine($"ON condition: {basicMatch.Groups[3].Value}");
        }
    }
} 