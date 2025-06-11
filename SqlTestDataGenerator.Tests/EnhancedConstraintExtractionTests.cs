using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Test cases để verify enhanced constraint extraction với IN, BETWEEN, NULL patterns
/// Tests Step 2 implementation: Critical missing patterns
/// </summary>
[TestClass]
[TestCategory("Enhanced-Extraction")]
public class EnhancedConstraintExtractionTests
{
    private ComprehensiveConstraintExtractor _extractor = null!;

    [TestInitialize]
    public void Setup()
    {
        _extractor = new ComprehensiveConstraintExtractor();
    }

    /// <summary>
    /// Test IN clause extraction - STRING LIST
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_InClauseStringList_ShouldExtractValues()
    {
        // Arrange
        var inClauseQuery = @"
            SELECT * FROM users u 
            WHERE u.department IN ('HR', 'IT', 'Finance')
              AND u.status IN ('ACTIVE', 'PENDING')";

        // Act
        var constraints = _extractor.ExtractAllConstraints(inClauseQuery);

        // Assert
        Console.WriteLine($"=== IN CLAUSE STRING LIST TEST RESULTS ===");
        Console.WriteLine($"Total Constraints: {constraints.GetTotalCount()}");
        Console.WriteLine($"IN Clause Constraints: {constraints.InClauseConstraints.Count}");
        
        foreach (var inConstraint in constraints.InClauseConstraints)
        {
            Console.WriteLine($"IN: {inConstraint.TableAlias}.{inConstraint.ColumnName} IN ({string.Join(", ", inConstraint.Values)}) - Type: {inConstraint.InClauseType}");
        }

        Assert.AreEqual(2, constraints.InClauseConstraints.Count, "Should extract 2 IN clause constraints");
        
        var departmentConstraint = constraints.InClauseConstraints.FirstOrDefault(c => c.ColumnName == "department");
        Assert.IsNotNull(departmentConstraint, "Should find department IN constraint");
        Assert.AreEqual("u", departmentConstraint.TableAlias, "Should have table alias 'u'");
        Assert.AreEqual("STRING_LIST", departmentConstraint.InClauseType, "Should be STRING_LIST type");
        Assert.AreEqual(3, departmentConstraint.Values.Count, "Should have 3 department values");
        CollectionAssert.Contains(departmentConstraint.Values, "HR", "Should contain HR");
        CollectionAssert.Contains(departmentConstraint.Values, "IT", "Should contain IT");
        CollectionAssert.Contains(departmentConstraint.Values, "Finance", "Should contain Finance");
        
        Console.WriteLine("✅ IN clause string list extraction test passed");
    }

    /// <summary>
    /// Test IN clause extraction - NUMERIC LIST
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_InClauseNumericList_ShouldExtractValues()
    {
        // Arrange
        var inClauseQuery = @"
            SELECT * FROM users u 
            WHERE u.status_id IN (1, 2, 3, 5)
              AND u.level IN (10, 20, 30)";

        // Act
        var constraints = _extractor.ExtractAllConstraints(inClauseQuery);

        // Assert
        Console.WriteLine($"=== IN CLAUSE NUMERIC LIST TEST RESULTS ===");
        Console.WriteLine($"IN Clause Constraints: {constraints.InClauseConstraints.Count}");
        
        foreach (var inConstraint in constraints.InClauseConstraints)
        {
            Console.WriteLine($"IN: {inConstraint.TableAlias}.{inConstraint.ColumnName} IN ({string.Join(", ", inConstraint.Values)}) - Type: {inConstraint.InClauseType}");
        }

        Assert.AreEqual(2, constraints.InClauseConstraints.Count, "Should extract 2 numeric IN constraints");
        
        var statusConstraint = constraints.InClauseConstraints.FirstOrDefault(c => c.ColumnName == "status_id");
        Assert.IsNotNull(statusConstraint, "Should find status_id IN constraint");
        Assert.AreEqual("NUMERIC_LIST", statusConstraint.InClauseType, "Should be NUMERIC_LIST type");
        Assert.AreEqual(4, statusConstraint.Values.Count, "Should have 4 status values");
        
        Console.WriteLine("✅ IN clause numeric list extraction test passed");
    }

    /// <summary>
    /// Test IN clause extraction - SUBQUERY
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_InClauseSubquery_ShouldExtractSubquery()
    {
        // Arrange
        var inClauseQuery = @"
            SELECT * FROM users u 
            WHERE u.role_id IN (SELECT id FROM active_roles WHERE level > 5)
              AND u.company_id IN (SELECT id FROM companies WHERE status = 'ACTIVE')";

        // Act
        var constraints = _extractor.ExtractAllConstraints(inClauseQuery);

        // Assert
        Console.WriteLine($"=== IN CLAUSE SUBQUERY TEST RESULTS ===");
        Console.WriteLine($"IN Clause Constraints: {constraints.InClauseConstraints.Count}");
        
        foreach (var inConstraint in constraints.InClauseConstraints)
        {
            Console.WriteLine($"IN: {inConstraint.TableAlias}.{inConstraint.ColumnName} IN ({inConstraint.SubQuery}) - Type: {inConstraint.InClauseType}");
        }

        Assert.AreEqual(2, constraints.InClauseConstraints.Count, "Should extract 2 subquery IN constraints");
        
        var roleConstraint = constraints.InClauseConstraints.FirstOrDefault(c => c.ColumnName == "role_id");
        Assert.IsNotNull(roleConstraint, "Should find role_id IN constraint");
        Assert.AreEqual("SUBQUERY", roleConstraint.InClauseType, "Should be SUBQUERY type");
        Assert.IsTrue(roleConstraint.SubQuery.Contains("SELECT id FROM active_roles"), "Should contain subquery");
        
        Console.WriteLine("✅ IN clause subquery extraction test passed");
    }

    /// <summary>
    /// Test BETWEEN constraint extraction
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_BetweenOperators_ShouldExtractRanges()
    {
        // Arrange
        var betweenQuery = @"
            SELECT * FROM users u 
            WHERE u.salary BETWEEN 5000000 AND 10000000
              AND u.hire_date BETWEEN '2020-01-01' AND '2023-12-31'
              AND u.age BETWEEN 25 AND 65";

        // Act
        var constraints = _extractor.ExtractAllConstraints(betweenQuery);

        // Assert
        Console.WriteLine($"=== BETWEEN OPERATORS TEST RESULTS ===");
        Console.WriteLine($"BETWEEN Constraints: {constraints.BetweenConstraints.Count}");
        
        foreach (var betweenConstraint in constraints.BetweenConstraints)
        {
            Console.WriteLine($"BETWEEN: {betweenConstraint.TableAlias}.{betweenConstraint.ColumnName} BETWEEN '{betweenConstraint.MinValue}' AND '{betweenConstraint.MaxValue}' - Type: {betweenConstraint.DataType}");
        }

        Assert.AreEqual(3, constraints.BetweenConstraints.Count, "Should extract 3 BETWEEN constraints");
        
        var salaryConstraint = constraints.BetweenConstraints.FirstOrDefault(c => c.ColumnName == "salary");
        Assert.IsNotNull(salaryConstraint, "Should find salary BETWEEN constraint");
        Assert.AreEqual("u", salaryConstraint.TableAlias, "Should have table alias 'u'");
        Assert.AreEqual("5000000", salaryConstraint.MinValue, "Should have min value 5000000");
        Assert.AreEqual("10000000", salaryConstraint.MaxValue, "Should have max value 10000000");
        Assert.AreEqual("NUMERIC", salaryConstraint.DataType, "Should be NUMERIC type");
        
        var dateConstraint = constraints.BetweenConstraints.FirstOrDefault(c => c.ColumnName == "hire_date");
        Assert.IsNotNull(dateConstraint, "Should find hire_date BETWEEN constraint");
        Assert.AreEqual("DATE", dateConstraint.DataType, "Should be DATE type");
        
        Console.WriteLine("✅ BETWEEN operators extraction test passed");
    }

    /// <summary>
    /// Test NULL constraint extraction
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_NullChecks_ShouldExtractNullConstraints()
    {
        // Arrange
        var nullCheckQuery = @"
            SELECT * FROM users u 
            WHERE u.deleted_at IS NULL
              AND u.phone IS NOT NULL
              AND u.email IS NOT NULL
              AND u.middle_name IS NULL";

        // Act
        var constraints = _extractor.ExtractAllConstraints(nullCheckQuery);

        // Assert
        Console.WriteLine($"=== NULL CHECK TEST RESULTS ===");
        Console.WriteLine($"NULL Constraints: {constraints.NullConstraints.Count}");
        
        foreach (var nullConstraint in constraints.NullConstraints)
        {
            Console.WriteLine($"NULL: {nullConstraint.TableAlias}.{nullConstraint.ColumnName} {nullConstraint.NullCheckType} - IsNull: {nullConstraint.IsNull}");
        }

        Assert.AreEqual(4, constraints.NullConstraints.Count, "Should extract 4 NULL constraints");
        
        var deletedAtConstraint = constraints.NullConstraints.FirstOrDefault(c => c.ColumnName == "deleted_at");
        Assert.IsNotNull(deletedAtConstraint, "Should find deleted_at NULL constraint");
        Assert.AreEqual("u", deletedAtConstraint.TableAlias, "Should have table alias 'u'");
        Assert.IsTrue(deletedAtConstraint.IsNull, "Should be IS NULL");
        Assert.AreEqual("IS_NULL", deletedAtConstraint.NullCheckType, "Should be IS_NULL type");
        
        var phoneConstraint = constraints.NullConstraints.FirstOrDefault(c => c.ColumnName == "phone");
        Assert.IsNotNull(phoneConstraint, "Should find phone NOT NULL constraint");
        Assert.IsFalse(phoneConstraint.IsNull, "Should be IS NOT NULL");
        Assert.AreEqual("IS_NOT_NULL", phoneConstraint.NullCheckType, "Should be IS_NOT_NULL type");
        
        Console.WriteLine("✅ NULL checks extraction test passed");
    }

    /// <summary>
    /// Test comprehensive extraction với all new patterns combined
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_AllNewPatternsCombined_ShouldExtractAll()
    {
        // Arrange - Complex query với all new patterns
        var complexQuery = @"
            SELECT u.id, u.username, u.email, c.name as company_name
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            WHERE u.department IN ('HR', 'IT', 'Finance')
              AND u.salary BETWEEN 5000000 AND 15000000
              AND u.deleted_at IS NULL
              AND u.phone IS NOT NULL
              AND u.status_id IN (1, 2, 3)
              AND u.hire_date BETWEEN '2020-01-01' AND '2024-12-31'
              AND c.name LIKE '%VNEXT%'";

        // Act
        var constraints = _extractor.ExtractAllConstraints(complexQuery);

        // Assert
        Console.WriteLine($"=== ALL NEW PATTERNS COMBINED TEST RESULTS ===");
        Console.WriteLine($"Total Constraints: {constraints.GetTotalCount()}");
        Console.WriteLine($"IN Clause: {constraints.InClauseConstraints.Count}");
        Console.WriteLine($"BETWEEN: {constraints.BetweenConstraints.Count}");
        Console.WriteLine($"NULL: {constraints.NullConstraints.Count}");
        Console.WriteLine($"LIKE: {constraints.LikePatterns.Count}");
        Console.WriteLine($"JOIN: {constraints.JoinConstraints.Count}");

        // Verify all pattern types are extracted
        Assert.IsTrue(constraints.InClauseConstraints.Count >= 2, "Should extract IN clause constraints");
        Assert.IsTrue(constraints.BetweenConstraints.Count >= 2, "Should extract BETWEEN constraints");
        Assert.IsTrue(constraints.NullConstraints.Count >= 2, "Should extract NULL constraints");
        Assert.IsTrue(constraints.LikePatterns.Count >= 1, "Should extract LIKE patterns");
        Assert.IsTrue(constraints.JoinConstraints.Count >= 1, "Should extract JOIN constraints");
        
        // Verify total count includes all new constraint types
        var expectedMinimum = constraints.InClauseConstraints.Count + constraints.BetweenConstraints.Count + 
                             constraints.NullConstraints.Count + constraints.LikePatterns.Count + 
                             constraints.JoinConstraints.Count;
        Assert.IsTrue(constraints.GetTotalCount() >= expectedMinimum, "Total count should include all constraint types");
        
        Console.WriteLine("✅ All new patterns combined extraction test passed");
        Console.WriteLine($"🎉 ENHANCED CONSTRAINT EXTRACTION SUCCESSFUL!");
    }
} 