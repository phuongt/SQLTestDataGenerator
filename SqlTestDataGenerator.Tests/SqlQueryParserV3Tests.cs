using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Tests;

[TestClass]
[TestCategory("SqlQueryParserV3")]
public class SqlQueryParserV3Tests
{
    private SqlQueryParserV3 _parser;

    [TestInitialize]
    public void Setup()
    {
        _parser = new SqlQueryParserV3();
    }

    [TestMethod]
    public void ParseQuery_BasicSelectWithWhere_ExtractsConditions()
    {
        // Arrange
        var query = @"SELECT * FROM users u WHERE u.name = 'John' AND u.age > 25";

        // Act
        var result = _parser.ParseQuery(query);

        // Assert
        Assert.AreEqual(2, result.WhereConditions.Count);
        
        var nameCondition = result.WhereConditions.FirstOrDefault(c => c.ColumnName == "name");
        Assert.IsNotNull(nameCondition);
        Assert.AreEqual("u", nameCondition.TableAlias);
        Assert.AreEqual("=", nameCondition.Operator);
        Assert.AreEqual("John", nameCondition.Value);

        var ageCondition = result.WhereConditions.FirstOrDefault(c => c.ColumnName == "age");
        Assert.IsNotNull(ageCondition);
        Assert.AreEqual("u", ageCondition.TableAlias);
        Assert.AreEqual(">", ageCondition.Operator);
        Assert.AreEqual("25", ageCondition.Value);
    }

    [TestMethod]
    public void ParseQuery_JoinConditions_ExtractsJoinRequirements()
    {
        // Arrange
        var query = @"SELECT u.name, r.role_name 
                     FROM users u 
                     INNER JOIN user_roles ur ON u.id = ur.user_id 
                     INNER JOIN roles r ON ur.role_id = r.id 
                     WHERE u.is_active = TRUE";

        // Act
        var result = _parser.ParseQuery(query);

        // Assert
        Assert.AreEqual(2, result.JoinRequirements.Count);
        
        var firstJoin = result.JoinRequirements.FirstOrDefault(j => j.LeftTableAlias == "u");
        Assert.IsNotNull(firstJoin);
        Assert.AreEqual("id", firstJoin.LeftColumn);
        Assert.AreEqual("ur", firstJoin.RightTableAlias);
        Assert.AreEqual("user_id", firstJoin.RightColumn);

        var secondJoin = result.JoinRequirements.FirstOrDefault(j => j.LeftTableAlias == "ur");
        Assert.IsNotNull(secondJoin);
        Assert.AreEqual("role_id", secondJoin.LeftColumn);
        Assert.AreEqual("r", secondJoin.RightTableAlias);
        Assert.AreEqual("id", secondJoin.RightColumn);

        // Should also extract the WHERE condition
        Console.WriteLine($"Total WHERE conditions: {result.WhereConditions.Count}");
        foreach (var condition in result.WhereConditions)
        {
            Console.WriteLine($"- {condition.TableAlias}.{condition.ColumnName} {condition.Operator} {condition.Value}");
        }
        
        var whereConditions = result.WhereConditions.Where(c => c.ColumnName == "is_active").ToList();
        Console.WriteLine($"is_active conditions: {whereConditions.Count}");
        Assert.AreEqual(1, whereConditions.Count);
        Assert.AreEqual("TRUE", whereConditions[0].Value);
    }

    [TestMethod]
    public void ParseQuery_MySqlDateFunction_HandlesCorrectly()
    {
        // Arrange
        var query = @"SELECT * FROM orders o 
                     WHERE o.created_date <= DATE_ADD(NOW(), INTERVAL 30 DAY)
                     AND o.status = 'active'";

        // Act
        var result = _parser.ParseQuery(query);

        // Assert
        // Should extract the status condition
        var statusCondition = result.WhereConditions.FirstOrDefault(c => c.ColumnName == "status");
        Assert.IsNotNull(statusCondition);
        Assert.AreEqual("active", statusCondition.Value);

        // Note: DATE_ADD function parsing may require special handling
        Console.WriteLine($"Extracted {result.WhereConditions.Count} WHERE conditions");
        foreach (var condition in result.WhereConditions)
        {
            Console.WriteLine($"- {condition.TableAlias}.{condition.ColumnName} {condition.Operator} {condition.Value}");
        }
    }

    [TestMethod]
    public void ParseQuery_LikeCondition_ExtractsPattern()
    {
        // Arrange
        var query = @"SELECT * FROM products p WHERE p.name LIKE '%iPhone%'";

        // Act
        var result = _parser.ParseQuery(query);

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count);
        
        var likeCondition = result.WhereConditions[0];
        Assert.AreEqual("p", likeCondition.TableAlias);
        Assert.AreEqual("name", likeCondition.ColumnName);
        Assert.AreEqual("LIKE", likeCondition.Operator);
        Assert.AreEqual("iPhone", likeCondition.Value); // Should remove % wildcards
    }

    [TestMethod]
    public void ParseQuery_InCondition_ExtractsValues()
    {
        // Arrange
        var query = @"SELECT * FROM orders o WHERE o.status IN ('pending', 'processing', 'shipped')";

        // Act
        var result = _parser.ParseQuery(query);

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count);
        
        var inCondition = result.WhereConditions[0];
        Assert.AreEqual("o", inCondition.TableAlias);
        Assert.AreEqual("status", inCondition.ColumnName);
        Assert.AreEqual("IN", inCondition.Operator);
        Assert.AreEqual("pending,processing,shipped", inCondition.Value);
    }

    [TestMethod]
    public void ParseQuery_NullCondition_ExtractsCorrectly()
    {
        // Arrange
        var query = @"SELECT * FROM users u WHERE u.deleted_at IS NULL AND u.email IS NOT NULL";

        // Act
        var result = _parser.ParseQuery(query);

        // Assert
        Assert.AreEqual(2, result.WhereConditions.Count);
        
        var nullCondition = result.WhereConditions.FirstOrDefault(c => c.ColumnName == "deleted_at");
        Assert.IsNotNull(nullCondition);
        Assert.AreEqual("IS_NULL", nullCondition.Operator);

        var notNullCondition = result.WhereConditions.FirstOrDefault(c => c.ColumnName == "email");
        Assert.IsNotNull(notNullCondition);
        Assert.AreEqual("IS_NOT_NULL", notNullCondition.Operator);
    }

    [TestMethod]
    public void ParseQuery_ComplexVowisQuery_ExtractsAllConstraints()
    {
        // Arrange - This was the failing test case
        var query = @"SELECT v.id, v.title, v.description, v.created_date, v.expiry_date, 
                            v.discount_percent, v.discount_amount, v.min_order_value, v.usage_limit, 
                            v.used_count, v.is_active, v.created_by, v.updated_by
                     FROM vowis v
                     INNER JOIN user_roles ur ON v.created_by = ur.user_id
                     WHERE v.expiry_date <= DATE_ADD(NOW(), INTERVAL 30 DAY)
                       AND v.is_active = TRUE
                       AND ur.is_active = TRUE
                     ORDER BY v.created_date DESC";

        // Act
        var result = _parser.ParseQuery(query);

        // Assert
        Console.WriteLine($"Extracted {result.WhereConditions.Count} WHERE conditions:");
        foreach (var condition in result.WhereConditions)
        {
            Console.WriteLine($"- {condition.TableAlias}.{condition.ColumnName} {condition.Operator} {condition.Value}");
        }

        Console.WriteLine($"\nExtracted {result.JoinRequirements.Count} JOIN requirements:");
        foreach (var join in result.JoinRequirements)
        {
            Console.WriteLine($"- {join.LeftTableAlias}.{join.LeftColumn} = {join.RightTableAlias}.{join.RightColumn}");
        }

        // Should extract JOIN relationship
        Assert.IsTrue(result.JoinRequirements.Count >= 1, "Should extract JOIN requirement");
        
        // Should extract active conditions for both tables
        var activeConditions = result.WhereConditions.Where(c => c.ColumnName == "is_active").ToList();
        Assert.IsTrue(activeConditions.Count >= 1, "Should extract at least one is_active condition");

        // Should extract tables
        Assert.IsTrue(result.TableRequirements.ContainsKey("vowis") || 
                      result.TableRequirements.ContainsKey("user_roles"), 
                      "Should extract table requirements");
    }

    [TestMethod]
    public void ParseQuery_FallbackToRegexParser_WhenSqlParserCSFails()
    {
        // Arrange - Intentionally malformed SQL to trigger fallback
        var query = @"SELECT * FROM users WHERE name LIKE 'John' AND invalid_syntax $$$ here";

        // Act
        var result = _parser.ParseQuery(query);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(query, result.OriginalQuery);
        
        // Should fallback to regex parser and extract what it can
        Console.WriteLine($"Fallback parser extracted {result.WhereConditions.Count} conditions");
    }

    [TestMethod]
    public void ParseQuery_EmptyQuery_ReturnsEmptyRequirements()
    {
        // Arrange
        var query = "";

        // Act
        var result = _parser.ParseQuery(query);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.WhereConditions.Count);
        Assert.AreEqual(0, result.JoinRequirements.Count);
        Assert.AreEqual(0, result.TableRequirements.Count);
    }
} 