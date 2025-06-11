using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Test cases để verify missing SQL patterns trong ComprehensiveConstraintExtractor
/// Identifies gaps và edge cases cần implement
/// </summary>
[TestClass]
[TestCategory("Missing-Patterns")]
public class MissingSqlPatternsTests
{
    private ComprehensiveConstraintExtractor _extractor = null!;

    [TestInitialize]
    public void Setup()
    {
        _extractor = new ComprehensiveConstraintExtractor();
    }

    /// <summary>
    /// Test IN clause patterns - CURRENTLY NOT SUPPORTED
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_InClausePatterns_ShouldExtractValues()
    {
        // Arrange - IN clause examples
        var inClauseQuery = @"
            SELECT * FROM users u 
            WHERE u.department IN ('HR', 'IT', 'Finance')
              AND u.status IN (1, 2, 3)
              AND u.role_id IN (SELECT id FROM active_roles)";

        // Act
        var constraints = _extractor.ExtractAllConstraints(inClauseQuery);

        // Assert - Document current behavior (expected to miss IN clauses)
        Console.WriteLine($"=== IN CLAUSE TEST RESULTS ===");
        Console.WriteLine($"Total Constraints: {constraints.GetTotalCount()}");
        Console.WriteLine($"WHERE Constraints: {constraints.WhereConstraints.Count}");
        
        foreach (var constraint in constraints.WhereConstraints)
        {
            Console.WriteLine($"Found: {constraint.TableAlias}.{constraint.ColumnName} {constraint.Operator} '{constraint.Value}'");
        }

        // EXPECTED: Currently should miss IN clause constraints
        var inConstraints = constraints.WhereConstraints.Where(c => c.Operator == "IN").ToList();
        Console.WriteLine($"❌ IN constraints found: {inConstraints.Count} (Expected: 0 until implemented)");
        
        // This will fail until IN clause support is added
        Assert.AreEqual(0, inConstraints.Count, "IN clauses not yet supported - this test documents the gap");
    }

    /// <summary>
    /// Test BETWEEN operators - CURRENTLY NOT SUPPORTED
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_BetweenOperators_ShouldExtractRanges()
    {
        // Arrange - BETWEEN examples
        var betweenQuery = @"
            SELECT * FROM users u 
            WHERE u.salary BETWEEN 5000000 AND 10000000
              AND u.hire_date BETWEEN '2020-01-01' AND '2023-12-31'";

        // Act
        var constraints = _extractor.ExtractAllConstraints(betweenQuery);

        // Assert - Document current behavior
        Console.WriteLine($"=== BETWEEN OPERATORS TEST RESULTS ===");
        Console.WriteLine($"Total Constraints: {constraints.GetTotalCount()}");
        
        var betweenConstraints = constraints.WhereConstraints.Where(c => c.Operator == "BETWEEN").ToList();
        Console.WriteLine($"❌ BETWEEN constraints found: {betweenConstraints.Count} (Expected: 0 until implemented)");
        
        Assert.AreEqual(0, betweenConstraints.Count, "BETWEEN operators not yet supported");
    }

    /// <summary>
    /// Test IS NULL / IS NOT NULL patterns - CURRENTLY NOT SUPPORTED
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_NullChecks_ShouldExtractNullConstraints()
    {
        // Arrange - NULL check examples
        var nullCheckQuery = @"
            SELECT * FROM users u 
            WHERE u.deleted_at IS NULL
              AND u.phone IS NOT NULL
              AND u.email IS NOT NULL";

        // Act
        var constraints = _extractor.ExtractAllConstraints(nullCheckQuery);

        // Assert - Document current behavior
        Console.WriteLine($"=== NULL CHECK TEST RESULTS ===");
        Console.WriteLine($"Total Constraints: {constraints.GetTotalCount()}");
        
        var nullConstraints = constraints.WhereConstraints.Where(c => 
            c.Operator == "IS_NULL" || c.Operator == "IS_NOT_NULL").ToList();
        Console.WriteLine($"❌ NULL constraints found: {nullConstraints.Count} (Expected: 0 until implemented)");
        
        Assert.AreEqual(0, nullConstraints.Count, "NULL checks not yet supported");
    }

    /// <summary>
    /// Test function calls trong WHERE clause - CURRENTLY LIMITED SUPPORT
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_FunctionCalls_ShouldExtractFunctionConstraints()
    {
        // Arrange - Function call examples
        var functionQuery = @"
            SELECT * FROM users u 
            WHERE LENGTH(u.username) > 5
              AND UPPER(u.email) LIKE '%@VNEXT.COM%'
              AND SUBSTRING(u.phone, 1, 3) = '084'";

        // Act
        var constraints = _extractor.ExtractAllConstraints(functionQuery);

        // Assert - Document current behavior
        Console.WriteLine($"=== FUNCTION CALLS TEST RESULTS ===");
        Console.WriteLine($"Total Constraints: {constraints.GetTotalCount()}");
        
        // May catch some patterns but miss function-specific logic
        foreach (var constraint in constraints.WhereConstraints)
        {
            Console.WriteLine($"Found: {constraint.TableAlias}.{constraint.ColumnName} {constraint.Operator} '{constraint.Value}'");
        }
        
        // Should miss advanced function call constraints
        var functionConstraints = constraints.WhereConstraints.Where(c => 
            c.Value.Contains("LENGTH") || c.Value.Contains("UPPER") || c.Value.Contains("SUBSTRING")).ToList();
        Console.WriteLine($"❌ Function-based constraints: {functionConstraints.Count}");
    }

    /// <summary>
    /// Test complex parenthetical logic - LIMITED SUPPORT
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_ComplexParentheses_ShouldHandleNestedLogic()
    {
        // Arrange - Complex nested conditions
        var complexQuery = @"
            SELECT * FROM users u 
            WHERE (u.first_name LIKE '%John%' OR u.last_name LIKE '%John%') 
              AND (u.department = 'IT' OR u.department = 'HR')
              AND NOT (u.status = 'INACTIVE' AND u.deleted_at IS NOT NULL)";

        // Act
        var constraints = _extractor.ExtractAllConstraints(complexQuery);

        // Assert - Document what's captured vs missed
        Console.WriteLine($"=== COMPLEX PARENTHESES TEST RESULTS ===");
        Console.WriteLine($"Total Constraints: {constraints.GetTotalCount()}");
        Console.WriteLine($"LIKE Patterns: {constraints.LikePatterns.Count}");
        Console.WriteLine($"WHERE Constraints: {constraints.WhereConstraints.Count}");
        
        foreach (var like in constraints.LikePatterns)
        {
            Console.WriteLine($"LIKE: {like.TableAlias}.{like.ColumnName} LIKE '{like.Pattern}'");
        }
        
        foreach (var where in constraints.WhereConstraints)
        {
            Console.WriteLine($"WHERE: {where.TableAlias}.{where.ColumnName} {where.Operator} '{where.Value}'");
        }
        
        // Should capture some but may miss logical grouping context
        Assert.IsTrue(constraints.GetTotalCount() > 0, "Should capture some constraints from complex query");
    }

    /// <summary>
    /// Test EXISTS subqueries - CURRENTLY NOT SUPPORTED
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_ExistsSubqueries_ShouldExtractExistsLogic()
    {
        // Arrange - EXISTS patterns
        var existsQuery = @"
            SELECT * FROM users u 
            WHERE EXISTS (SELECT 1 FROM user_roles ur WHERE ur.user_id = u.id AND ur.is_active = 1)
              AND NOT EXISTS (SELECT 1 FROM user_suspensions us WHERE us.user_id = u.id)";

        // Act
        var constraints = _extractor.ExtractAllConstraints(existsQuery);

        // Assert - Document current behavior
        Console.WriteLine($"=== EXISTS SUBQUERIES TEST RESULTS ===");
        Console.WriteLine($"Total Constraints: {constraints.GetTotalCount()}");
        
        // Currently should miss EXISTS logic
        var existsConstraints = constraints.WhereConstraints.Where(c => 
            c.Operator == "EXISTS" || c.Operator == "NOT_EXISTS").ToList();
        Console.WriteLine($"❌ EXISTS constraints found: {existsConstraints.Count} (Expected: 0 until implemented)");
        
        Assert.AreEqual(0, existsConstraints.Count, "EXISTS subqueries not yet supported");
    }

    /// <summary>
    /// Test edge case: Empty WHERE clause
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_EmptyWhere_ShouldHandleGracefully()
    {
        // Arrange
        var noWhereQuery = "SELECT * FROM users u ORDER BY u.created_at";

        // Act
        var constraints = _extractor.ExtractAllConstraints(noWhereQuery);

        // Assert
        Console.WriteLine($"=== EMPTY WHERE TEST RESULTS ===");
        Console.WriteLine($"Total Constraints: {constraints.GetTotalCount()}");
        
        Assert.AreEqual(0, constraints.GetTotalCount(), "Should handle queries without WHERE clause");
    }

    /// <summary>
    /// Test edge case: Malformed SQL
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_MalformedSQL_ShouldHandleGracefully()
    {
        // Arrange - Intentionally broken SQL
        var malformedQuery = @"
            SELECT * FROM users u 
            WHERE u.name = AND u.status 
            LIKE '%test OR u.id = ";

        // Act & Assert - Should not throw exception
        try
        {
            var constraints = _extractor.ExtractAllConstraints(malformedQuery);
            Console.WriteLine($"=== MALFORMED SQL TEST RESULTS ===");
            Console.WriteLine($"Total Constraints: {constraints.GetTotalCount()}");
            Console.WriteLine("✅ Handled malformed SQL gracefully without exception");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception on malformed SQL: {ex.Message}");
            Assert.Fail($"Should handle malformed SQL gracefully, but threw: {ex.Message}");
        }
    }
} 