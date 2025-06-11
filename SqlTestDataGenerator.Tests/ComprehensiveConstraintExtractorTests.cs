using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using System.Linq;
using Serilog;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Comprehensive test suite cho ComprehensiveConstraintExtractor
/// Tests tất cả SQL constraint patterns và edge cases
/// </summary>
[TestClass]
[TestCategory("ComprehensiveConstraints")]
public class ComprehensiveConstraintExtractorTests
{
    private ComprehensiveConstraintExtractor _extractor;

    [TestInitialize]
    public void Setup()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
            
        _extractor = new ComprehensiveConstraintExtractor();
    }

    [TestCleanup]
    public void Cleanup()
    {
        Log.CloseAndFlush();
    }

    #region WHERE Constraint Tests

    [TestMethod]
    public void ExtractWhereConstraints_WithEquality_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT u.id, u.name 
            FROM users u 
            WHERE u.status = 'ACTIVE' AND u.department = 'HR'";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(2, constraints.WhereConstraints.Count);
        
        var statusConstraint = constraints.WhereConstraints.FirstOrDefault(c => c.ColumnName == "status");
        Assert.IsNotNull(statusConstraint);
        Assert.AreEqual("u", statusConstraint.TableAlias);
        Assert.AreEqual("=", statusConstraint.Operator);
        Assert.AreEqual("ACTIVE", statusConstraint.Value);

        var deptConstraint = constraints.WhereConstraints.FirstOrDefault(c => c.ColumnName == "department");
        Assert.IsNotNull(deptConstraint);
        Assert.AreEqual("HR", deptConstraint.Value);
    }

    [TestMethod]
    public void ExtractWhereConstraints_WithComparison_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM products p 
            WHERE p.price >= 100 AND p.quantity < 50";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(2, constraints.WhereConstraints.Count);
        
        var priceConstraint = constraints.WhereConstraints.FirstOrDefault(c => c.ColumnName == "price");
        Assert.IsNotNull(priceConstraint);
        Assert.AreEqual(">=", priceConstraint.Operator);
        Assert.AreEqual("100", priceConstraint.Value);

        var quantityConstraint = constraints.WhereConstraints.FirstOrDefault(c => c.ColumnName == "quantity");
        Assert.IsNotNull(quantityConstraint);
        Assert.AreEqual("<", quantityConstraint.Operator);
        Assert.AreEqual("50", quantityConstraint.Value);
    }

    #endregion

    #region JOIN Constraint Tests

    [TestMethod]
    public void ExtractJoinConstraints_WithAdditionalConditions_ShouldExtractBoth()
    {
        // Arrange
        var sql = @"
            SELECT u.name, r.name 
            FROM users u 
            INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = 'TRUE'
            INNER JOIN roles r ON ur.role_id = r.id";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.IsTrue(constraints.JoinConstraints.Count >= 3); // Main JOIN + additional conditions

        // Check main JOIN conditions
        var userRoleJoin = constraints.JoinConstraints.FirstOrDefault(j => 
            j.LeftColumn == "id" && j.RightColumn == "user_id");
        Assert.IsNotNull(userRoleJoin);

        // Check additional constraint in JOIN ON clause
        var activeConstraint = constraints.JoinConstraints.FirstOrDefault(j => 
            j.LeftColumn == "is_active" && j.Value == "TRUE");
        Assert.IsNotNull(activeConstraint);
        Assert.AreEqual("ur", activeConstraint.LeftTableAlias);
        Assert.AreEqual("JOIN_CONDITION", activeConstraint.Source);
    }

    #endregion

    #region LIKE Pattern Tests

    [TestMethod]
    public void ExtractLikePatterns_WithVariousPatterns_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM users u 
            WHERE u.email LIKE '%@company.com' 
            AND u.name LIKE 'John%' 
            AND u.username LIKE '%admin%'";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(3, constraints.LikePatterns.Count);

        var emailPattern = constraints.LikePatterns.FirstOrDefault(p => p.ColumnName == "email");
        Assert.IsNotNull(emailPattern);
        Assert.AreEqual("ENDS_WITH", emailPattern.PatternType);
        Assert.AreEqual("@company.com", emailPattern.RequiredValue);

        var namePattern = constraints.LikePatterns.FirstOrDefault(p => p.ColumnName == "name");
        Assert.IsNotNull(namePattern);
        Assert.AreEqual("STARTS_WITH", namePattern.PatternType);
        Assert.AreEqual("John", namePattern.RequiredValue);

        var usernamePattern = constraints.LikePatterns.FirstOrDefault(p => p.ColumnName == "username");
        Assert.IsNotNull(usernamePattern);
        Assert.AreEqual("CONTAINS", usernamePattern.PatternType);
        Assert.AreEqual("admin", usernamePattern.RequiredValue);
    }

    #endregion

    #region Date Constraint Tests

    [TestMethod]
    public void ExtractDateConstraints_WithYearFunction_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM employees e 
            WHERE YEAR(e.birth_date) = 1989 
            AND e.hire_date >= DATE_ADD(NOW(), INTERVAL 1 YEAR)";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(2, constraints.DateConstraints.Count);

        var yearConstraint = constraints.DateConstraints.FirstOrDefault(d => d.ConstraintType == "YEAR_EQUALS");
        Assert.IsNotNull(yearConstraint);
        Assert.AreEqual("birth_date", yearConstraint.ColumnName);
        Assert.AreEqual("1989", yearConstraint.Value);

        var intervalConstraint = constraints.DateConstraints.FirstOrDefault(d => d.ConstraintType == "DATE_INTERVAL");
        Assert.IsNotNull(intervalConstraint);
        Assert.AreEqual("hire_date", intervalConstraint.ColumnName);
        Assert.AreEqual(">=", intervalConstraint.Operator);
        Assert.AreEqual("1_YEAR", intervalConstraint.Value);
    }

    #endregion

    #region Boolean Constraint Tests

    [TestMethod]
    public void ExtractBooleanConstraints_WithVariousFormats_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM users u 
            WHERE u.is_active = TRUE 
            AND u.is_verified = 1 
            AND u.is_deleted = FALSE
            AND u.is_admin = 0";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(4, constraints.BooleanConstraints.Count);

        var activeConstraint = constraints.BooleanConstraints.FirstOrDefault(b => b.ColumnName == "is_active");
        Assert.IsNotNull(activeConstraint);
        Assert.IsTrue(activeConstraint.BooleanValue);

        var verifiedConstraint = constraints.BooleanConstraints.FirstOrDefault(b => b.ColumnName == "is_verified");
        Assert.IsNotNull(verifiedConstraint);
        Assert.IsTrue(verifiedConstraint.BooleanValue);

        var deletedConstraint = constraints.BooleanConstraints.FirstOrDefault(b => b.ColumnName == "is_deleted");
        Assert.IsNotNull(deletedConstraint);
        Assert.IsFalse(deletedConstraint.BooleanValue);

        var adminConstraint = constraints.BooleanConstraints.FirstOrDefault(b => b.ColumnName == "is_admin");
        Assert.IsNotNull(adminConstraint);
        Assert.IsFalse(adminConstraint.BooleanValue);
    }

    #endregion

    #region IN Clause Tests

    [TestMethod]
    public void ExtractInClauseConstraints_WithStringList_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM employees e 
            WHERE e.department IN ('HR', 'Finance', 'Engineering') 
            AND e.status = 'active'";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(1, result.InClauseConstraints.Count, "Should extract 1 IN clause constraint");
        
        var inConstraint = result.InClauseConstraints[0];
        Assert.AreEqual("e", inConstraint.TableAlias);
        Assert.AreEqual("department", inConstraint.ColumnName);
        Assert.AreEqual("STRING_LIST", inConstraint.InClauseType);
        Assert.AreEqual(3, inConstraint.Values.Count);
        Assert.IsTrue(inConstraint.Values.Contains("HR"));
        Assert.IsTrue(inConstraint.Values.Contains("Finance"));
        Assert.IsTrue(inConstraint.Values.Contains("Engineering"));
    }

    [TestMethod]
    public void ExtractInClauseConstraints_WithNumericList_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM products p 
            WHERE p.category_id IN (1, 2, 3, 5, 8)";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(1, result.InClauseConstraints.Count);
        
        var inConstraint = result.InClauseConstraints[0];
        Assert.AreEqual("p", inConstraint.TableAlias);
        Assert.AreEqual("category_id", inConstraint.ColumnName);
        Assert.AreEqual("NUMERIC_LIST", inConstraint.InClauseType);
        Assert.AreEqual(5, inConstraint.Values.Count);
        Assert.IsTrue(inConstraint.Values.Contains("1"));
        Assert.IsTrue(inConstraint.Values.Contains("8"));
    }

    [TestMethod]
    public void ExtractInClauseConstraints_WithSubquery_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM orders o 
            WHERE o.customer_id IN (
                SELECT customer_id FROM customers WHERE region = 'North'
            )";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(1, result.InClauseConstraints.Count);
        
        var inConstraint = result.InClauseConstraints[0];
        Assert.AreEqual("o", inConstraint.TableAlias);
        Assert.AreEqual("customer_id", inConstraint.ColumnName);
        Assert.AreEqual("SUBQUERY", inConstraint.InClauseType);
        Assert.IsTrue(inConstraint.SubQuery.Contains("SELECT customer_id FROM customers"));
        Assert.AreEqual(0, inConstraint.Values.Count);
    }

    [TestMethod]
    public void ExtractInClauseConstraints_WithoutTableAlias_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM products 
            WHERE status IN ('active', 'pending', 'discontinued')";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(1, result.InClauseConstraints.Count);
        
        var inConstraint = result.InClauseConstraints[0];
        Assert.AreEqual("", inConstraint.TableAlias);
        Assert.AreEqual("status", inConstraint.ColumnName);
        Assert.AreEqual("STRING_LIST", inConstraint.InClauseType);
        Assert.AreEqual(3, inConstraint.Values.Count);
    }

    #endregion

    #region BETWEEN Constraint Tests

    [TestMethod]
    public void ExtractBetweenConstraints_WithNumericValues_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM products p 
            WHERE p.price BETWEEN 100 AND 500 
            AND p.stock > 0";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(1, result.BetweenConstraints.Count, "Should extract 1 BETWEEN constraint");
        
        var betweenConstraint = result.BetweenConstraints[0];
        Assert.AreEqual("p", betweenConstraint.TableAlias);
        Assert.AreEqual("price", betweenConstraint.ColumnName);
        Assert.AreEqual("100", betweenConstraint.MinValue);
        Assert.AreEqual("500", betweenConstraint.MaxValue);
        Assert.AreEqual("NUMERIC", betweenConstraint.DataType);
    }

    [TestMethod]
    public void ExtractBetweenConstraints_WithDateValues_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM orders o 
            WHERE o.order_date BETWEEN '2023-01-01' AND '2023-12-31'";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(1, result.BetweenConstraints.Count);
        
        var betweenConstraint = result.BetweenConstraints[0];
        Assert.AreEqual("o", betweenConstraint.TableAlias);
        Assert.AreEqual("order_date", betweenConstraint.ColumnName);
        Assert.AreEqual("2023-01-01", betweenConstraint.MinValue);
        Assert.AreEqual("2023-12-31", betweenConstraint.MaxValue);
        Assert.AreEqual("DATE", betweenConstraint.DataType);
    }

    [TestMethod]
    public void ExtractBetweenConstraints_WithStringValues_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM customers 
            WHERE customer_name BETWEEN 'A' AND 'M'";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(1, result.BetweenConstraints.Count);
        
        var betweenConstraint = result.BetweenConstraints[0];
        Assert.AreEqual("", betweenConstraint.TableAlias);
        Assert.AreEqual("customer_name", betweenConstraint.ColumnName);
        Assert.AreEqual("A", betweenConstraint.MinValue);
        Assert.AreEqual("M", betweenConstraint.MaxValue);
        Assert.AreEqual("STRING", betweenConstraint.DataType);
    }

    #endregion

    #region NULL Constraint Tests

    [TestMethod]
    public void ExtractNullConstraints_WithIsNull_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM employees e 
            WHERE e.manager_id IS NULL 
            AND e.department = 'HR'";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(1, result.NullConstraints.Count, "Should extract 1 NULL constraint");
        
        var nullConstraint = result.NullConstraints[0];
        Assert.AreEqual("e", nullConstraint.TableAlias);
        Assert.AreEqual("manager_id", nullConstraint.ColumnName);
        Assert.IsTrue(nullConstraint.IsNull);
        Assert.AreEqual("IS_NULL", nullConstraint.NullCheckType);
    }

    [TestMethod]
    public void ExtractNullConstraints_WithIsNotNull_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM products p 
            WHERE p.description IS NOT NULL 
            AND p.category_id > 0";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(1, result.NullConstraints.Count);
        
        var nullConstraint = result.NullConstraints[0];
        Assert.AreEqual("p", nullConstraint.TableAlias);
        Assert.AreEqual("description", nullConstraint.ColumnName);
        Assert.IsFalse(nullConstraint.IsNull);
        Assert.AreEqual("IS_NOT_NULL", nullConstraint.NullCheckType);
    }

    [TestMethod]
    public void ExtractNullConstraints_WithoutTableAlias_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM customers 
            WHERE email IS NOT NULL 
            AND phone IS NULL";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(2, result.NullConstraints.Count, "Should extract 2 NULL constraints");
        
        var emailConstraint = result.NullConstraints.FirstOrDefault(c => c.ColumnName == "email");
        Assert.IsNotNull(emailConstraint);
        Assert.AreEqual("", emailConstraint.TableAlias);
        Assert.IsFalse(emailConstraint.IsNull);
        Assert.AreEqual("IS_NOT_NULL", emailConstraint.NullCheckType);

        var phoneConstraint = result.NullConstraints.FirstOrDefault(c => c.ColumnName == "phone");
        Assert.IsNotNull(phoneConstraint);
        Assert.AreEqual("", phoneConstraint.TableAlias);
        Assert.IsTrue(phoneConstraint.IsNull);
        Assert.AreEqual("IS_NULL", phoneConstraint.NullCheckType);
    }

    #endregion

    #region EXISTS Constraint Tests

    [TestMethod]
    public void ExtractExistsConstraints_WithExists_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM customers c 
            WHERE EXISTS (
                SELECT 1 FROM orders o WHERE o.customer_id = c.id AND o.status = 'completed'
            )";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(1, result.ExistsConstraints.Count, "Should extract 1 EXISTS constraint");
        
        var existsConstraint = result.ExistsConstraints[0];
        Assert.IsTrue(existsConstraint.IsExists);
        Assert.AreEqual("EXISTS", existsConstraint.ExistsType);
        Assert.IsTrue(existsConstraint.SubQuery.Contains("SELECT 1 FROM orders"));
        Assert.IsTrue(existsConstraint.SubQuery.Contains("o.customer_id = c.id"));
    }

    [TestMethod]
    public void ExtractExistsConstraints_WithNotExists_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM customers c 
            WHERE NOT EXISTS (
                SELECT 1 FROM orders o WHERE o.customer_id = c.id AND o.status = 'cancelled'
            )";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(1, result.ExistsConstraints.Count, "Should extract 1 NOT EXISTS constraint");
        
        var existsConstraint = result.ExistsConstraints[0];
        Assert.IsFalse(existsConstraint.IsExists);
        Assert.AreEqual("NOT_EXISTS", existsConstraint.ExistsType);
        Assert.IsTrue(existsConstraint.SubQuery.Contains("SELECT 1 FROM orders"));
        Assert.IsTrue(existsConstraint.SubQuery.Contains("o.status = 'cancelled'"));
    }

    [TestMethod]
    public void ExtractExistsConstraints_WithMultipleExists_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM customers c 
            WHERE EXISTS (SELECT 1 FROM orders o WHERE o.customer_id = c.id)
            AND NOT EXISTS (SELECT 1 FROM complaints comp WHERE comp.customer_id = c.id)";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(2, result.ExistsConstraints.Count, "Should extract 2 EXISTS constraints");
        
        var existsConstraint = result.ExistsConstraints.FirstOrDefault(e => e.IsExists);
        Assert.IsNotNull(existsConstraint);
        Assert.AreEqual("EXISTS", existsConstraint.ExistsType);

        var notExistsConstraint = result.ExistsConstraints.FirstOrDefault(e => !e.IsExists);
        Assert.IsNotNull(notExistsConstraint);
        Assert.AreEqual("NOT_EXISTS", notExistsConstraint.ExistsType);
    }

    #endregion

    #region Complex Query Tests

    [TestMethod]
    public void ExtractAllConstraints_WithComplexQuery_ShouldExtractAllTypes()
    {
        // Arrange
        var sql = @"
            SELECT u.id, u.name, c.name, r.name
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id AND c.is_active = TRUE
            INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_primary = 1
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE u.status = 'ACTIVE'
            AND u.age BETWEEN 25 AND 65
            AND u.department IN ('HR', 'IT', 'Finance')
            AND u.email LIKE '%@company.com'
            AND u.salary >= 50000
            AND u.manager_id IS NOT NULL
            AND YEAR(u.hire_date) = 2023";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.IsTrue(constraints.WhereConstraints.Count >= 2); // status, salary
        Assert.IsTrue(constraints.JoinConstraints.Count >= 4); // Main JOINs + additional conditions
        Assert.AreEqual(1, constraints.LikePatterns.Count);
        Assert.AreEqual(1, constraints.BetweenConstraints.Count);
        Assert.AreEqual(1, constraints.InClauseConstraints.Count);
        Assert.AreEqual(1, constraints.NullConstraints.Count);
        Assert.AreEqual(1, constraints.DateConstraints.Count);

        Assert.IsTrue(constraints.GetTotalCount() >= 10); // Should have many constraints
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void ExtractAllConstraints_WithoutTableAlias_ShouldExtractCorrectly()
    {
        // Arrange
        var sql = @"
            SELECT * FROM users 
            WHERE status = 'ACTIVE' 
            AND age BETWEEN 18 AND 65
            AND department IN ('HR', 'IT')
            AND email IS NOT NULL";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sql);

        // Debug logging
        System.Console.WriteLine($"WHERE constraints: {constraints.WhereConstraints.Count}");
        System.Console.WriteLine($"BETWEEN constraints: {constraints.BetweenConstraints.Count}");
        System.Console.WriteLine($"IN constraints: {constraints.InClauseConstraints.Count}");
        System.Console.WriteLine($"NULL constraints: {constraints.NullConstraints.Count}");
        
        foreach (var wc in constraints.WhereConstraints)
        {
            System.Console.WriteLine($"WHERE: {wc.TableAlias}.{wc.ColumnName} {wc.Operator} {wc.Value}");
        }
        
        foreach (var bc in constraints.BetweenConstraints)
        {
            System.Console.WriteLine($"BETWEEN: {bc.TableAlias}.{bc.ColumnName} BETWEEN {bc.MinValue} AND {bc.MaxValue}");
        }

        // Assert - Should still extract constraints even without table aliases
        Assert.IsTrue(constraints.WhereConstraints.Count >= 1, $"Expected >= 1 WHERE constraints, got {constraints.WhereConstraints.Count}");
        Assert.AreEqual(1, constraints.BetweenConstraints.Count);
        Assert.AreEqual(1, constraints.InClauseConstraints.Count);
        Assert.AreEqual(1, constraints.NullConstraints.Count);
    }

    [TestMethod]
    public void Debug_ExtractConstraints_NoTableAlias_Simple()
    {
        // Arrange - Simple test to debug
        var sql = "SELECT * FROM users WHERE status = 'ACTIVE'";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sql);

        // Assert
        System.Console.WriteLine($"Simple test - WHERE constraints: {constraints.WhereConstraints.Count}");
        Assert.AreEqual(1, constraints.WhereConstraints.Count, "Should extract 1 WHERE constraint");
    }

    [TestMethod]
    public void ExtractAllConstraints_WithEmptyQuery_ShouldReturnEmptyConstraints()
    {
        // Arrange
        var sql = "";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.IsNotNull(constraints);
        Assert.AreEqual(0, constraints.GetTotalCount());
    }

    [TestMethod]
    public void ExtractAllConstraints_WithCommentsAndWhitespace_ShouldCleanAndExtract()
    {
        // Arrange
        var sql = @"
            -- This is a comment
            SELECT u.id, u.name 
            FROM users u 
            /* Multi-line comment
               with details */
            WHERE u.status = 'ACTIVE' 
            -- Another comment
            AND u.age >= 18";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.AreEqual(2, constraints.WhereConstraints.Count);
    }

    #endregion

    #region Constraint Utility Tests

    [TestMethod]
    public void GetConstraintsForTable_WithMultipleConstraintTypes_ShouldReturnCorrectConstraints()
    {
        // Arrange
        var sql = @"
            SELECT * FROM employees e 
            WHERE e.department IN ('HR', 'IT') 
            AND e.salary BETWEEN 50000 AND 100000
            AND e.manager_id IS NOT NULL";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);
        var employeeConstraints = result.GetConstraintsForTable("e");

        // Assert
        Assert.IsTrue(employeeConstraints.Count >= 3, "Should find constraints for table alias 'e'");
        
        // Verify different constraint types are included
        var constraintTypes = employeeConstraints.Select(c => c.Source).Distinct().ToList();
        Assert.IsTrue(constraintTypes.Contains("IN_CLAUSE") || constraintTypes.Contains("BETWEEN") || constraintTypes.Contains("NULL_CHECK"));
    }

    [TestMethod]
    public void GetConstraintsForColumn_WithSpecificColumn_ShouldReturnOnlyColumnConstraints()
    {
        // Arrange
        var sql = @"
            SELECT * FROM employees e 
            WHERE e.department IN ('HR', 'IT') 
            AND e.salary BETWEEN 50000 AND 100000";

        // Act
        var result = _extractor.ExtractAllConstraints(sql);
        var departmentConstraints = result.GetConstraintsForColumn("e", "department");

        // Assert
        Assert.AreEqual(1, departmentConstraints.Count, "Should find exactly 1 constraint for e.department");
        Assert.AreEqual("department", departmentConstraints[0].ColumnName);
    }

    #endregion
} 