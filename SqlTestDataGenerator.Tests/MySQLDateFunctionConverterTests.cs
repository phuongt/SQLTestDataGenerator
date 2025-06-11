using Microsoft.Extensions.Logging.Abstractions;
using SqlTestDataGenerator.Core.Services;

namespace SqlTestDataGenerator.Tests;

[TestClass]
[TestCategory("MySQLDateConverter")]
public class MySQLDateFunctionConverterTests
{
    private MySQLDateFunctionConverter _converter = null!;

    [TestInitialize]
    public void Setup()
    {
        _converter = new MySQLDateFunctionConverter(NullLogger<MySQLDateFunctionConverter>.Instance);
    }

    #region datetime() Conversion Tests

    [TestMethod]
    public void ConvertToMySQLSyntax_DatetimeDays_ShouldConvertToDateAdd()
    {
        // Arrange
        var query = "WHERE expires_at <= datetime('now', '+30 days')";

        // Act
        var result = _converter.ConvertToMySQLSyntax(query);

        // Assert
        Assert.AreEqual("WHERE expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY)", result);
        Console.WriteLine($"✅ Converted: {query} -> {result}");
    }

    [TestMethod]
    public void ConvertToMySQLSyntax_DatetimeMonths_ShouldConvertToDateAdd()
    {
        // Arrange
        var query = "WHERE created_at >= datetime('now', '-6 months')";

        // Act
        var result = _converter.ConvertToMySQLSyntax(query);

        // Assert
        Assert.AreEqual("WHERE created_at >= DATE_SUB(NOW(), INTERVAL 6 MONTH)", result);
        Console.WriteLine($"✅ Converted: {query} -> {result}");
    }

    [TestMethod]
    public void ConvertToMySQLSyntax_DatetimeYears_ShouldConvertToDateAdd()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE hire_date <= datetime('now', '-2 years')";

        // Act
        var result = _converter.ConvertToMySQLSyntax(query);

        // Assert
        Assert.AreEqual("SELECT * FROM users WHERE hire_date <= DATE_SUB(NOW(), INTERVAL 2 YEAR)", result);
        Console.WriteLine($"✅ Converted: {query} -> {result}");
    }

    [TestMethod]
    public void ConvertToMySQLSyntax_DatetimePluralUnits_ShouldNormalize()
    {
        // Arrange
        var query = "WHERE expires_at <= datetime('now', '+60 days') AND created_at >= datetime('now', '-3 months')";

        // Act
        var result = _converter.ConvertToMySQLSyntax(query);

        // Assert
        var expected = "WHERE expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY) AND created_at >= DATE_SUB(NOW(), INTERVAL 3 MONTH)";
        Assert.AreEqual(expected, result);
        Console.WriteLine($"✅ Converted: {query} -> {result}");
    }

    #endregion

    #region CASE Statement Tests

    [TestMethod]
    public void ConvertToMySQLSyntax_CaseStatementWithDatetime_ShouldConvert()
    {
        // Arrange
        var query = @"
            SELECT CASE
                WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= datetime('now', '+30 days') THEN 'Sắp hết hạn vai trò'
                ELSE 'Đang làm việc'
            END AS work_status";

        // Act
        var result = _converter.ConvertToMySQLSyntax(query);

        // Assert
        Assert.IsTrue(result.Contains("DATE_ADD(NOW(), INTERVAL 30 DAY)"), "Should contain MySQL DATE_ADD syntax");
        Assert.IsFalse(result.Contains("datetime('now', '+30 days')"), "Should not contain datetime syntax");
        Assert.IsTrue(result.Contains("'Đã nghỉ việc'"), "Should preserve Vietnamese text");
        
        Console.WriteLine($"✅ CASE statement converted successfully");
        Console.WriteLine($"Result:\n{result}");
    }

    [TestMethod]
    public void ConvertToMySQLSyntax_ComplexCaseWithMultipleDateFunctions_ShouldConvertAll()
    {
        // Arrange
        var query = @"
            SELECT CASE
                WHEN created_at <= datetime('now', '-1 year') THEN 'Old'
                WHEN expires_at <= datetime('now', '+30 days') THEN 'Expiring Soon'
                ELSE 'Active'
            END AS status";

        // Act
        var result = _converter.ConvertToMySQLSyntax(query);

        // Assert
        Assert.IsTrue(result.Contains("DATE_SUB(NOW(), INTERVAL 1 YEAR)"), "Should convert first datetime");
        Assert.IsTrue(result.Contains("DATE_ADD(NOW(), INTERVAL 30 DAY)"), "Should convert second datetime");
        Assert.IsFalse(result.Contains("datetime('now'"), "Should not contain any datetime syntax");
        
        Console.WriteLine($"✅ Complex CASE statement converted successfully");
    }

    [TestMethod]
    public void ConvertToMySQLSyntax_MixedSyntax_ShouldConvertOnlyNonMySQL()
    {
        // Arrange - Mix of datetime và MySQL syntax
        var query = @"
            WHERE expires_at <= datetime('now', '+30 days') 
            AND created_at >= DATE_SUB(NOW(), INTERVAL 1 MONTH)
            AND updated_at <= datetime('now', '-7 days')";

        // Act
        var result = _converter.ConvertToMySQLSyntax(query);

        // Assert
        Assert.IsTrue(result.Contains("DATE_ADD(NOW(), INTERVAL 30 DAY)"), "Should convert first datetime");
        Assert.IsTrue(result.Contains("DATE_SUB(NOW(), INTERVAL 1 MONTH)"), "Should preserve existing MySQL syntax");
        Assert.IsTrue(result.Contains("DATE_SUB(NOW(), INTERVAL 7 DAY)"), "Should convert third datetime");
        Assert.IsFalse(result.Contains("datetime('now'"), "Should not contain any datetime syntax");
        
        Console.WriteLine($"✅ Mixed syntax converted correctly");
    }

    [TestMethod]
    public void ConvertToMySQLSyntax_NestedCaseStatements_ShouldConvertAll()
    {
        // Arrange
        var query = @"
            SELECT CASE
                WHEN priority = 1 THEN 
                    CASE 
                        WHEN expires_at <= datetime('now', '+7 days') THEN 'Urgent'
                        ELSE 'High'
                    END
                WHEN expires_at <= datetime('now', '+30 days') THEN 'Medium'
                ELSE 'Low'
            END AS priority_level";

        // Act
        var result = _converter.ConvertToMySQLSyntax(query);

        // Assert
        Assert.IsTrue(result.Contains("DATE_ADD(NOW(), INTERVAL 7 DAY)"), "Should convert nested datetime");
        Assert.IsTrue(result.Contains("DATE_ADD(NOW(), INTERVAL 30 DAY)"), "Should convert outer datetime");
        Assert.IsFalse(result.Contains("datetime('now'"), "Should not contain datetime syntax");
        
        Console.WriteLine($"✅ Nested CASE statements converted successfully");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void ConvertToMySQLSyntax_EmptyOrNullQuery_ShouldReturnUnchanged()
    {
        // Arrange & Act & Assert
        Assert.AreEqual("", _converter.ConvertToMySQLSyntax(""));
        Assert.AreEqual(string.Empty, _converter.ConvertToMySQLSyntax(null!));
        Assert.AreEqual("   ", _converter.ConvertToMySQLSyntax("   "));
        Console.WriteLine($"✅ Empty/null queries handled correctly");
    }

    #endregion

    #region Validation Tests

    [TestMethod]
    public void ValidateMySQLSyntax_ValidMySQLQuery_ShouldReturnTrue()
    {
        // Arrange
        var validQuery = "WHERE expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY)";

        // Act & Assert
        Assert.IsTrue(_converter.ValidateMySQLSyntax(validQuery));
        Console.WriteLine($"✅ Valid MySQL syntax recognized");
    }

    [TestMethod]
    public void ValidateMySQLSyntax_InvalidQuery_ShouldReturnFalse()
    {
        // Arrange
        var invalidQuery = "WHERE expires_at <= datetime('now', '+30 days')";

        // Act & Assert
        Assert.IsFalse(_converter.ValidateMySQLSyntax(invalidQuery));
        Console.WriteLine($"✅ Invalid syntax detected and rejected");
    }

    [TestMethod]
    public void ValidateMySQLSyntax_SubstrFunction_ShouldReturnFalse()
    {
        // Arrange
        var queryWithSubstr = "WHERE substr(name, 1, 5) = 'Admin'";

        // Act & Assert
        Assert.IsFalse(_converter.ValidateMySQLSyntax(queryWithSubstr));
        Console.WriteLine($"✅ substr function detected and rejected");
    }

    #endregion

    #region Suggestions Tests

    [TestMethod]
    public void GetConversionSuggestions_InvalidQuery_ShouldReturnSuggestions()
    {
        // Arrange
        var invalidQuery = "WHERE expires_at <= datetime('now', '+30 days') AND substr(name, 1, 5) = 'Admin'";

        // Act
        var suggestions = _converter.GetConversionSuggestions(invalidQuery);

        // Assert
        Assert.IsTrue(suggestions.Count >= 2, "Should provide multiple suggestions");
        Assert.IsTrue(suggestions.Any(s => s.Description.Contains("DATE_ADD")), "Should suggest DATE_ADD");
        Assert.IsTrue(suggestions.Any(s => s.Description.Contains("SUBSTRING")), "Should suggest SUBSTRING");
        
        Console.WriteLine($"✅ Found {suggestions.Count} conversion suggestions");
        foreach (var suggestion in suggestions)
        {
            Console.WriteLine($"   - {suggestion.Description}: {suggestion.OriginalSyntax} -> {suggestion.MySQLSyntax}");
        }
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public void ConvertToMySQLSyntax_BusinessQuery_ShouldFixAllIssues()
    {
        // Arrange - Real business query với mixed issues
        var businessQuery = @"
            SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth,
                   c.name AS company_name, c.code AS company_code,
                   r.name AS role_name, r.code AS role_code,
                   CASE 
                       WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                       WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= datetime('now', '+30 days') THEN 'Sắp hết hạn vai trò'
                       ELSE 'Đang làm việc'
                   END AS work_status
            FROM users u
            INNER JOIN companies c ON u.company_id = c.id  
            INNER JOIN user_roles ur ON u.id = ur.user_id
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE c.name LIKE '%VNEXT%'
              AND u.first_name LIKE '%Phương%'
              AND u.last_name LIKE '%Phương%'  
              AND ur.expires_at IS NOT NULL 
              AND ur.expires_at <= datetime('now', '+30 days')
              AND u.is_active = 0
              AND ur.is_active = 1
            ORDER BY u.created_at DESC
            LIMIT 1";

        // Act
        var result = _converter.ConvertToMySQLSyntax(businessQuery);

        // Assert
        Assert.IsTrue(_converter.ValidateMySQLSyntax(result), "Result should be valid MySQL");
        Assert.IsFalse(result.Contains("datetime('now'"), "Should not contain datetime");
        Assert.IsTrue(result.Contains("DATE_ADD(NOW(), INTERVAL 30 DAY)"), "Should contain MySQL DATE_ADD");
        Assert.IsTrue(result.Contains("'Đã nghỉ việc'"), "Should preserve Vietnamese text");
        
        Console.WriteLine($"✅ Business query converted successfully to MySQL");
        Console.WriteLine($"Original contained datetime syntax, converted version is clean");
    }

    #endregion
} 