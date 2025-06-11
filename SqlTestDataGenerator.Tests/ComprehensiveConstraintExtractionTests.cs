using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Test cases để verify comprehensive constraint extraction
/// Focuses on TC001 query để ensure VNEXT và ur.is_active = TRUE được extract đúng
/// </summary>
[TestClass]
[TestCategory("Constraint-Extraction")]
public class ComprehensiveConstraintExtractionTests
{
    private ComprehensiveConstraintExtractor _extractor = null!;

    [TestInitialize]
    public void Setup()
    {
        _extractor = new ComprehensiveConstraintExtractor();
    }

    /// <summary>
    /// Test extraction của TC001 query để verify VNEXT và ur.is_active constraints
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_TC001_ComplexVowisSQL_ShouldExtractVNEXTAndIsActive()
    {
        // Arrange - TC001 query từ test case
        var tc001Query = @"
            SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date, 
                   c.NAME AS company_name, c.code AS company_code, r.NAME AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
                   CASE 
                       WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                       WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn vai trò'
                       ELSE 'Đang làm việc'
                   END AS work_status
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = TRUE
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
              AND YEAR(u.date_of_birth) = 1989
              AND c.NAME LIKE '%VNEXT%'
              AND r.code LIKE '%DD%'
              AND (u.is_active = 0 OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY))
            ORDER BY ur.expires_at ASC, u.created_at DESC";

        // Act
        var constraints = _extractor.ExtractAllConstraints(tc001Query);

        // Assert
        Console.WriteLine($"=== CONSTRAINT EXTRACTION TEST RESULTS ===");
        Console.WriteLine($"Total Constraints: {constraints.GetTotalCount()}");
        Console.WriteLine($"LIKE Patterns: {constraints.LikePatterns.Count}");
        Console.WriteLine($"Boolean Constraints: {constraints.BooleanConstraints.Count}");
        Console.WriteLine($"Date Constraints: {constraints.DateConstraints.Count}");
        Console.WriteLine($"JOIN Constraints: {constraints.JoinConstraints.Count}");
        Console.WriteLine($"WHERE Constraints: {constraints.WhereConstraints.Count}");

        // 🎯 CRITICAL: Check VNEXT pattern extraction
        var vnextPattern = constraints.LikePatterns.FirstOrDefault(p => p.RequiredValue.Contains("VNEXT"));
        Assert.IsNotNull(vnextPattern, "Should extract VNEXT LIKE pattern from c.NAME LIKE '%VNEXT%'");
        Assert.AreEqual("c", vnextPattern.TableAlias, "VNEXT pattern should be for table alias 'c'");
        Assert.AreEqual("NAME", vnextPattern.ColumnName, "VNEXT pattern should be for column 'NAME'");
        Assert.AreEqual("VNEXT", vnextPattern.RequiredValue, "Should extract 'VNEXT' as required value");
        Console.WriteLine($"✅ VNEXT Pattern: {vnextPattern.TableAlias}.{vnextPattern.ColumnName} LIKE '{vnextPattern.Pattern}' -> '{vnextPattern.RequiredValue}'");

        // 🎯 CRITICAL: Check ur.is_active = TRUE extraction từ JOIN condition
        var isActiveConstraint = constraints.JoinConstraints.FirstOrDefault(j => 
            j.LeftTableAlias == "ur" && j.LeftColumn == "is_active" && j.Value == "TRUE");
        Assert.IsNotNull(isActiveConstraint, "Should extract ur.is_active = TRUE from JOIN condition");
        Assert.AreEqual("ur", isActiveConstraint.LeftTableAlias, "Should be table alias 'ur'");
        Assert.AreEqual("is_active", isActiveConstraint.LeftColumn, "Should be column 'is_active'");
        Assert.AreEqual("TRUE", isActiveConstraint.Value, "Should extract value 'TRUE'");
        Console.WriteLine($"✅ JOIN Boolean: {isActiveConstraint.LeftTableAlias}.{isActiveConstraint.LeftColumn} = {isActiveConstraint.Value}");

        // Check Phương pattern
        var phuongPattern = constraints.LikePatterns.FirstOrDefault(p => p.RequiredValue.Contains("Phương"));
        Assert.IsNotNull(phuongPattern, "Should extract Phương LIKE pattern");
        Console.WriteLine($"✅ Phương Pattern: {phuongPattern.TableAlias}.{phuongPattern.ColumnName} LIKE '{phuongPattern.Pattern}' -> '{phuongPattern.RequiredValue}'");

        // Check DD pattern
        var ddPattern = constraints.LikePatterns.FirstOrDefault(p => p.RequiredValue.Contains("DD"));
        Assert.IsNotNull(ddPattern, "Should extract DD LIKE pattern");
        Console.WriteLine($"✅ DD Pattern: {ddPattern.TableAlias}.{ddPattern.ColumnName} LIKE '{ddPattern.Pattern}' -> '{ddPattern.RequiredValue}'");

        // Check year constraint
        var yearConstraint = constraints.DateConstraints.FirstOrDefault(d => d.ConstraintType == "YEAR_EQUALS");
        Assert.IsNotNull(yearConstraint, "Should extract YEAR(u.date_of_birth) = 1989 constraint");
        Assert.AreEqual("1989", yearConstraint.Value, "Should extract year 1989");
        Console.WriteLine($"✅ Year Constraint: YEAR({yearConstraint.TableAlias}.{yearConstraint.ColumnName}) = {yearConstraint.Value}");

        // Final validation
        Assert.IsTrue(constraints.GetTotalCount() >= 4, "Should extract at least 4 critical constraints");
        
        Console.WriteLine("🎉 ALL CONSTRAINT EXTRACTION TESTS PASSED!");
    }

    /// <summary>
    /// Test extraction của simple LIKE patterns
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_SimpleLikePatterns_ShouldExtractCorrectly()
    {
        // Arrange
        var sqlQuery = "SELECT * FROM companies WHERE name LIKE '%VNEXT%' AND code LIKE 'DD_%'";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sqlQuery);

        // Assert
        Console.WriteLine($"=== DEBUG LIKE PATTERNS ===");
        Console.WriteLine($"Total LIKE patterns found: {constraints.LikePatterns.Count}");
        foreach (var pattern in constraints.LikePatterns)
        {
            Console.WriteLine($"Pattern: {pattern.TableAlias}.{pattern.ColumnName} LIKE '{pattern.Pattern}' -> Required: '{pattern.RequiredValue}', Type: {pattern.PatternType}");
        }
        
        Assert.AreEqual(2, constraints.LikePatterns.Count, "Should extract 2 LIKE patterns");
        
        var vnextPattern = constraints.LikePatterns.FirstOrDefault(p => p.RequiredValue == "VNEXT");
        Assert.IsNotNull(vnextPattern, "Should find VNEXT pattern");
        Assert.AreEqual("CONTAINS", vnextPattern.PatternType, "VNEXT pattern should be CONTAINS type");
        
        var ddPattern = constraints.LikePatterns.FirstOrDefault(p => p.RequiredValue == "DD");
        Assert.IsNotNull(ddPattern, "Should find DD pattern"); 
        Assert.AreEqual("STARTS_WITH", ddPattern.PatternType, "DD pattern should be STARTS_WITH type");
        
        Console.WriteLine("✅ Simple LIKE patterns extraction test passed");
    }

    /// <summary>
    /// Test extraction của boolean constraints
    /// </summary>
    [TestMethod]
    public void TestExtractConstraints_BooleanConstraints_ShouldExtractCorrectly()
    {
        // Arrange
        var sqlQuery = "SELECT * FROM users WHERE is_active = 1 AND verified = TRUE";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sqlQuery);

        // Assert
        Assert.AreEqual(2, constraints.BooleanConstraints.Count, "Should extract 2 boolean constraints");
        
        var isActiveConstraint = constraints.BooleanConstraints.First(b => b.ColumnName == "is_active");
        Assert.AreEqual("1", isActiveConstraint.Value);
        Assert.IsTrue(isActiveConstraint.BooleanValue);
        
        var verifiedConstraint = constraints.BooleanConstraints.First(b => b.ColumnName == "verified");
        Assert.AreEqual("TRUE", verifiedConstraint.Value);
        Assert.IsTrue(verifiedConstraint.BooleanValue);
        
        Console.WriteLine("✅ Boolean constraints extraction test passed");
    }
} 