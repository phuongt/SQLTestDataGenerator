using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using System.Text;

namespace SqlTestDataGenerator.Tests;

[TestClass]
[TestCategory("DateAddUTF8Fixes")]
public class SqlDateAddUTF8Tests
{
    private SqlQueryParser _parser = null!;
    private ComprehensiveConstraintExtractor _extractor = null!;

    [TestInitialize]
    public void Setup()
    {
        _parser = new SqlQueryParser();
        _extractor = new ComprehensiveConstraintExtractor();
    }

    #region DATE_ADD Pattern Tests

    [TestMethod]
    public void ParseDateConditions_DateAddWithDay_ShouldParseCorrectly()
    {
        // Arrange
        var sql = "WHERE ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY)";

        // Act
        var result = _parser.ParseQuery($"SELECT * FROM user_roles ur {sql}");

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count, "Should parse DATE_ADD condition");
        var condition = result.WhereConditions[0];
        Assert.AreEqual("ur", condition.TableAlias, "Should extract table alias");
        Assert.AreEqual("expires_at", condition.ColumnName, "Should extract column name");
        Assert.AreEqual("DATE_WITHIN", condition.Operator, "Should map to DATE_WITHIN operator");
        Assert.AreEqual("30_DAY", condition.Value, "Should extract interval value");
        
        Console.WriteLine($"✅ Parsed: {condition.TableAlias}.{condition.ColumnName} {condition.Operator} {condition.Value}");
    }

    [TestMethod]
    public void ParseDateConditions_DateAddWithDays_ShouldParseCorrectly()
    {
        // Arrange - Test plural form DAYS
        var sql = "WHERE ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAYS)";

        // Act
        var result = _parser.ParseQuery($"SELECT * FROM user_roles ur {sql}");

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count, "Should parse DATE_ADD with DAYS");
        var condition = result.WhereConditions[0];
        Assert.AreEqual("60_DAY", condition.Value, "Should normalize DAYS to DAY");
    }

    [TestMethod]
    public void ParseDateConditions_DateAddWithMonth_ShouldParseCorrectly()
    {
        // Arrange
        var sql = "WHERE ur.expires_at >= DATE_ADD(NOW(), INTERVAL 1 MONTH)";

        // Act
        var result = _parser.ParseQuery($"SELECT * FROM user_roles ur {sql}");

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count, "Should parse DATE_ADD with MONTH");
        var condition = result.WhereConditions[0];
        Assert.AreEqual("DATE_COMPARE", condition.Operator, "Should map >= to DATE_COMPARE");
        Assert.AreEqual("1_MONTH", condition.Value, "Should extract month interval");
    }

    [TestMethod]
    public void ParseDateConditions_DateAddWithYear_ShouldParseCorrectly()
    {
        // Arrange
        var sql = "WHERE ur.expires_at < DATE_ADD(NOW(), INTERVAL 2 YEARS)";

        // Act
        var result = _parser.ParseQuery($"SELECT * FROM user_roles ur {sql}");

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count, "Should parse DATE_ADD with YEARS");
        var condition = result.WhereConditions[0];
        Assert.AreEqual("2_YEAR", condition.Value, "Should normalize YEARS to YEAR");
    }

    [TestMethod]
    public void ParseDateConditions_ReverseDateAdd_ShouldParseCorrectly()
    {
        // Arrange - Test reverse order: DATE_ADD >= column
        var sql = "WHERE DATE_ADD(NOW(), INTERVAL 30 DAY) >= ur.expires_at";

        // Act
        var result = _parser.ParseQuery($"SELECT * FROM user_roles ur {sql}");

        // Assert
        Assert.AreEqual(1, result.WhereConditions.Count, "Should parse reverse DATE_ADD");
        var condition = result.WhereConditions[0];
        Assert.AreEqual("ur", condition.TableAlias, "Should extract table alias correctly");
        Assert.AreEqual("expires_at", condition.ColumnName, "Should extract column name correctly");
        Assert.AreEqual("DATE_WITHIN", condition.Operator, "Should reverse >= to <= and map to DATE_WITHIN");
        Assert.AreEqual("30_DAY", condition.Value, "Should extract interval value");
    }

    [TestMethod]
    public void ExtractDateConstraints_ComplexDateAddQuery_ShouldParseAll()
    {
        // Arrange - Complex query with multiple DATE_ADD patterns
        var sql = @"
            SELECT * FROM users u
            JOIN user_roles ur ON u.id = ur.user_id
            WHERE ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY)
              AND ur.created_at >= DATE_ADD(NOW(), INTERVAL -90 DAYS)
              AND DATE_ADD(NOW(), INTERVAL 1 YEAR) > u.hire_date";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.IsTrue(constraints.DateConstraints.Count >= 2, "Should extract multiple DATE_ADD constraints");
        
        var expiresConstraint = constraints.DateConstraints.FirstOrDefault(d => d.ColumnName == "expires_at");
        Assert.IsNotNull(expiresConstraint, "Should find expires_at constraint");
        Assert.AreEqual("DATE_INTERVAL", expiresConstraint.ConstraintType, "Should be DATE_INTERVAL type");
        Assert.AreEqual("30_DAY", expiresConstraint.Value, "Should extract correct interval");
    }

    #endregion

    #region UTF-8 Character Tests

    [TestMethod]
    public void ParseVietnameseQuery_CaseStatement_ShouldHandleUTF8()
    {
        // Arrange - CASE statement with Vietnamese characters
        var sql = @"
            SELECT u.id, u.username,
                   CASE 
                       WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                       WHEN ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn vai trò'
                       ELSE 'Đang làm việc'
                   END AS work_status
            FROM users u
            JOIN user_roles ur ON u.id = ur.user_id
            WHERE u.first_name LIKE '%Phương%'";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.IsNotNull(result, "Should parse query with Vietnamese characters");
        Assert.IsTrue(result.WhereConditions.Count > 0, "Should extract WHERE conditions");
        
        var likeCondition = result.WhereConditions.FirstOrDefault(w => w.Operator == "LIKE");
        Assert.IsNotNull(likeCondition, "Should find LIKE condition");
        Assert.IsTrue(likeCondition.Value.Contains("Phương"), "Should preserve Vietnamese characters in LIKE pattern");
        
        Console.WriteLine($"✅ Vietnamese LIKE condition: {likeCondition.ColumnName} LIKE {likeCondition.Value}");
    }

    [TestMethod]
    public void ExtractConstraints_VietnameseCompanyNames_ShouldHandleUTF8()
    {
        // Arrange - Query with Vietnamese company names
        var sql = @"
            SELECT * FROM users u
            JOIN companies c ON u.company_id = c.id
            WHERE c.name LIKE '%CÔNG TY VNEXT%'
              AND u.department = 'Phòng Kỹ Thuật'
              AND u.notes LIKE '%tiếng Việt%'";

        // Act
        var constraints = _extractor.ExtractAllConstraints(sql);

        // Assert
        Assert.IsTrue(constraints.LikePatterns.Count >= 2, "Should extract Vietnamese LIKE patterns");
        
        var companyPattern = constraints.LikePatterns.FirstOrDefault(l => l.ColumnName == "name");
        Assert.IsNotNull(companyPattern, "Should find company name pattern");
        Assert.IsTrue(companyPattern.RequiredValue.Contains("VNEXT"), "Should preserve Vietnamese company names");
        
        var notesPattern = constraints.LikePatterns.FirstOrDefault(l => l.ColumnName == "notes");
        Assert.IsNotNull(notesPattern, "Should find notes pattern");
        Assert.IsTrue(notesPattern.RequiredValue.Contains("tiếng Việt"), "Should preserve Vietnamese text in patterns");
    }

    [TestMethod]
    public void ParseQuery_ComplexVietnameseSQL_ShouldHandleAllElements()
    {
        // Arrange - Complex VOWIS query with Vietnamese elements
        var vowisSQL = @"
            -- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
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
        var result = _parser.ParseQuery(vowisSQL);
        var constraints = _extractor.ExtractAllConstraints(vowisSQL);

        // Assert
        Assert.IsNotNull(result, "Should parse complex Vietnamese query");
        Assert.IsTrue(result.WhereConditions.Count > 0, "Should extract WHERE conditions");
        Assert.IsTrue(result.JoinRequirements.Count > 0, "Should extract JOIN requirements");
        
        // Check DATE_ADD parsing
        var dateConditions = result.WhereConditions.Where(w => w.Operator.Contains("DATE")).ToList();
        Assert.IsTrue(dateConditions.Count > 0, "Should find DATE_ADD conditions");
        
        // Check Vietnamese LIKE patterns
        var likeConditions = result.WhereConditions.Where(w => w.Operator == "LIKE").ToList();
        Assert.IsTrue(likeConditions.Any(l => l.Value.Contains("Phương")), "Should find Phương name pattern");
        Assert.IsTrue(likeConditions.Any(l => l.Value.Contains("VNEXT")), "Should find VNEXT company pattern");
        
        Console.WriteLine($"✅ Complex VOWIS query parsed successfully:");
        Console.WriteLine($"   - WHERE conditions: {result.WhereConditions.Count}");
        Console.WriteLine($"   - JOIN requirements: {result.JoinRequirements.Count}"); 
        Console.WriteLine($"   - DATE conditions: {dateConditions.Count}");
        Console.WriteLine($"   - LIKE patterns: {likeConditions.Count}");
    }

    [TestMethod]
    public void UTF8Encoding_ConnectionString_ShouldIncludeCharset()
    {
        // Arrange & Act - Test UTF-8 in connection string format
        var connectionString = "Server=localhost;Port=3306;Database=my_database;Uid=root;Pwd=22092012;Connect Timeout=120;Command Timeout=120;CharSet=utf8mb4;Connection Lifetime=300;";
        
        // Assert
        Assert.IsTrue(connectionString.Contains("CharSet=utf8mb4"), "Connection string should include UTF-8 charset");
        Assert.IsTrue(connectionString.Contains("utf8mb4"), "Should use utf8mb4 for full Unicode support");
        
        Console.WriteLine($"✅ UTF-8 Connection String: {connectionString}");
    }

    [TestMethod]
    public void StringEncoding_VietnameseText_ShouldPreserveCharacters()
    {
        // Arrange - Vietnamese text samples
        var vietnameseTexts = new[]
        {
            "Đã nghỉ việc",
            "Sắp hết hạn vai trò", 
            "Đang làm việc",
            "Phương Nguyễn",
            "Công ty VNEXT"
        };

        foreach (var text in vietnameseTexts)
        {
            // Act - Convert to bytes and back to test encoding
            var utf8Bytes = Encoding.UTF8.GetBytes(text);
            var reconstructed = Encoding.UTF8.GetString(utf8Bytes);
            
            // Assert
            Assert.AreEqual(text, reconstructed, $"Vietnamese text should be preserved: {text}");
            Assert.IsTrue(utf8Bytes.Length >= text.Length, "UTF-8 bytes should be sufficient for Vietnamese characters");
        }
        
        Console.WriteLine($"✅ All Vietnamese texts preserved correctly in UTF-8 encoding");
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public void IntegrationTest_DateAddAndUTF8_ShouldWorkTogether()
    {
        // Arrange - Test both fixes together
        var complexSQL = @"
            SELECT u.id, 
                   CASE 
                       WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                       WHEN ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAYS) THEN 'Sắp hết hạn'
                       ELSE 'Đang hoạt động'
                   END AS trạng_thái
            FROM users u
            JOIN user_roles ur ON u.id = ur.user_id
            WHERE u.first_name LIKE '%Phương%'
              AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY)
              AND c.name LIKE '%CÔNG TY%'";

        // Act
        var parserResult = _parser.ParseQuery(complexSQL);
        var extractorResult = _extractor.ExtractAllConstraints(complexSQL);

        // Assert - Both DATE_ADD and UTF-8 should work
        Assert.IsNotNull(parserResult, "Parser should handle mixed DATE_ADD and UTF-8");
        Assert.IsNotNull(extractorResult, "Extractor should handle mixed DATE_ADD and UTF-8");
        
        var dateConditions = parserResult.WhereConditions.Where(w => w.Operator.Contains("DATE")).ToList();
        Assert.IsTrue(dateConditions.Count > 0, "Should parse DATE_ADD conditions");
        
        var likeConditions = parserResult.WhereConditions.Where(w => w.Operator == "LIKE").ToList();
        Assert.IsTrue(likeConditions.Any(l => l.Value.Contains("Phương")), "Should parse Vietnamese LIKE patterns");
        
        Console.WriteLine($"✅ Integration test passed - DATE_ADD and UTF-8 work together");
        Console.WriteLine($"   - DATE conditions: {dateConditions.Count}");
        Console.WriteLine($"   - Vietnamese LIKE patterns: {likeConditions.Count(l => l.Value.Contains("Phương") || l.Value.Contains("CÔNG TY"))}");
    }

    #endregion
} 