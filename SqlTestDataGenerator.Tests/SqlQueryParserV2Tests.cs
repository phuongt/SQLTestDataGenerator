using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using Serilog;

namespace SqlTestDataGenerator.Tests;

[TestClass]
public class SqlQueryParserV2Tests
{
    private SqlQueryParserV2 _parser;

    [TestInitialize]
    public void Setup()
    {
        // Configure Serilog for testing
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        _parser = new SqlQueryParserV2();
    }

    [TestCleanup]
    public void Cleanup()
    {
        Log.CloseAndFlush();
    }

    [TestMethod]
    [TestCategory("ScriptDom")]
    public void ParseQuery_SimpleSelect_ExtractsBasicInfo()
    {
        // Arrange
        var sql = "SELECT id, name FROM users WHERE age > 25";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(sql, result.OriginalQuery);
        Assert.IsTrue(result.TableRequirements.ContainsKey("users"));
        Assert.AreEqual(1, result.WhereConditions.Count);
        
        var condition = result.WhereConditions.First();
        Assert.AreEqual("age", condition.ColumnName);
        Assert.AreEqual(">", condition.Operator);
        Assert.AreEqual("25", condition.Value);
    }

    [TestMethod]
    [TestCategory("ScriptDom")]
    public void ParseQuery_WithTableAlias_ExtractsCorrectAlias()
    {
        // Arrange
        var sql = "SELECT u.id, u.name FROM users u WHERE u.age > 25";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.TableRequirements.ContainsKey("users"));
        Assert.AreEqual(1, result.WhereConditions.Count);
        
        var condition = result.WhereConditions.First();
        Assert.AreEqual("u", condition.TableAlias);
        Assert.AreEqual("age", condition.ColumnName);
        Assert.AreEqual(">", condition.Operator);
        Assert.AreEqual("25", condition.Value);
    }

    [TestMethod]
    [TestCategory("ScriptDom")]
    public void ParseQuery_LikeCondition_ExtractsCleanValue()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE first_name LIKE '%Phương%'";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.WhereConditions.Count);
        
        var condition = result.WhereConditions.First();
        Assert.AreEqual("first_name", condition.ColumnName);
        Assert.AreEqual("LIKE", condition.Operator);
        Assert.AreEqual("Phương", condition.Value); // Should remove % wildcards
    }

    [TestMethod]
    [TestCategory("ScriptDom")]
    public void ParseQuery_InCondition_ExtractsValueList()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE department IN ('IT', 'HR', 'Finance')";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.WhereConditions.Count);
        
        var condition = result.WhereConditions.First();
        Assert.AreEqual("department", condition.ColumnName);
        Assert.AreEqual("IN", condition.Operator);
        Assert.IsTrue(condition.Value.Contains("IT"));
        Assert.IsTrue(condition.Value.Contains("HR"));
        Assert.IsTrue(condition.Value.Contains("Finance"));
    }

    [TestMethod]
    [TestCategory("ScriptDom")]
    public void ParseQuery_NullCondition_ExtractsCorrectly()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE email IS NOT NULL";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.WhereConditions.Count);
        
        var condition = result.WhereConditions.First();
        Assert.AreEqual("email", condition.ColumnName);
        Assert.AreEqual("IS_NOT_NULL", condition.Operator);
    }

    [TestMethod]
    [TestCategory("ScriptDom")]
    public void ParseQuery_WithJoin_ExtractsJoinRequirements()
    {
        // Arrange
        var sql = @"
            SELECT u.id, u.name, r.name as role_name 
            FROM users u 
            INNER JOIN user_roles ur ON u.id = ur.user_id
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE u.is_active = 1";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.TableRequirements.ContainsKey("users"));
        Assert.IsTrue(result.TableRequirements.ContainsKey("user_roles"));
        Assert.IsTrue(result.TableRequirements.ContainsKey("roles"));
        
        Assert.IsTrue(result.JoinRequirements.Count >= 2);
        
        // Check first join: u.id = ur.user_id
        var firstJoin = result.JoinRequirements.FirstOrDefault(j => 
            j.LeftTableAlias == "u" && j.LeftColumn == "id" && 
            j.RightTableAlias == "ur" && j.RightColumn == "user_id");
        Assert.IsNotNull(firstJoin);
        
        // Should also have WHERE condition
        Assert.IsTrue(result.WhereConditions.Any(w => w.ColumnName == "is_active"));
    }

    [TestMethod]
    [TestCategory("ScriptDom")]
    public void ParseQuery_ComplexVowisQuery_ExtractsAllConstraints()
    {
        // Arrange - TC001 complex query
        var sql = @"
            SELECT u.id, u.username, u.first_name, u.last_name, u.email, u.date_of_birth, u.salary, u.department, u.hire_date, 
                   c.NAME AS company_name, c.code AS company_code, r.NAME AS role_name, r.code AS role_code, ur.expires_at AS role_expires,
                   CASE 
                       WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                       WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn quyền'
                       WHEN u.hire_date >= DATE_SUB(NOW(), INTERVAL 90 DAY) THEN 'Nhân viên mới'
                       ELSE 'Đang làm việc'
                   END AS employment_status,
                   YEAR(u.date_of_birth) AS birth_year
            FROM users u
            INNER JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = TRUE
            INNER JOIN roles r ON ur.role_id = r.id
            INNER JOIN companies c ON u.company_id = c.id
            WHERE (u.first_name LIKE '%Phương%' OR u.last_name LIKE '%Phương%')
              AND YEAR(u.date_of_birth) = 1989
              AND u.department IN ('HR', 'IT', 'Finance')
              AND u.salary >= 5000000
              AND ur.expires_at IS NOT NULL
              AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY)
              AND u.is_active = 1
            ORDER BY u.salary DESC, u.hire_date ASC";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.IsNotNull(result);
        
        // Check tables
        Assert.IsTrue(result.TableRequirements.ContainsKey("users"));
        Assert.IsTrue(result.TableRequirements.ContainsKey("user_roles"));
        Assert.IsTrue(result.TableRequirements.ContainsKey("roles"));
        Assert.IsTrue(result.TableRequirements.ContainsKey("companies"));
        
        // Check JOIN requirements  
        Assert.IsTrue(result.JoinRequirements.Count >= 3);
        
        // Check WHERE conditions
        Assert.IsTrue(result.WhereConditions.Count >= 5);
        
        // Check specific conditions
        var nameCondition = result.WhereConditions.FirstOrDefault(w => 
            w.ColumnName.Contains("name") && w.Operator == "LIKE" && w.Value.Contains("Phương"));
        Assert.IsNotNull(nameCondition, "Should find LIKE condition for Phương");
        
        var departmentCondition = result.WhereConditions.FirstOrDefault(w => 
            w.ColumnName == "department" && w.Operator == "IN");
        Assert.IsNotNull(departmentCondition, "Should find IN condition for department");
        
        var salaryCondition = result.WhereConditions.FirstOrDefault(w => 
            w.ColumnName == "salary" && w.Operator == ">=" && w.Value == "5000000");
        Assert.IsNotNull(salaryCondition, "Should find salary >= 5000000 condition");
        
        var activeCondition = result.WhereConditions.FirstOrDefault(w => 
            w.ColumnName == "is_active" && w.Operator == "=" && w.Value == "1");
        Assert.IsNotNull(activeCondition, "Should find is_active = 1 condition");
    }

    [TestMethod]
    [TestCategory("ScriptDom")]
    public void ParseQuery_WithJoinAdditionalConditions_ExtractsBothJoinAndWhere()
    {
        // Arrange - Test case from .common-defects
        var sql = @"
            SELECT ur.*, u.username 
            FROM user_roles ur 
            INNER JOIN users u ON ur.user_id = u.id AND ur.is_active = TRUE
            WHERE u.department = 'IT'";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.IsNotNull(result);
        
        // Should extract JOIN condition: ur.user_id = u.id
        var joinCondition = result.JoinRequirements.FirstOrDefault(j => 
            j.LeftTableAlias == "ur" && j.LeftColumn == "user_id" && 
            j.RightTableAlias == "u" && j.RightColumn == "id");
        Assert.IsNotNull(joinCondition, "Should extract main JOIN condition");
        
        // Should extract JOIN additional condition: ur.is_active = TRUE
        var joinActiveCondition = result.WhereConditions.FirstOrDefault(w => 
            w.TableAlias == "ur" && w.ColumnName == "is_active" && w.Value == "TRUE");
        Assert.IsNotNull(joinActiveCondition, "Should extract ur.is_active = TRUE from JOIN ON");
        
        // Should extract WHERE condition: u.department = 'IT'
        var deptCondition = result.WhereConditions.FirstOrDefault(w => 
            w.TableAlias == "u" && w.ColumnName == "department" && w.Value == "IT");
        Assert.IsNotNull(deptCondition, "Should extract department condition from WHERE");
    }

    [TestMethod]
    [TestCategory("ScriptDom")]
    public void ParseQuery_InvalidSQL_FallsBackToRegexParser()
    {
        // Arrange
        var sql = "SELET * FORM users WERE age > 25"; // Intentionally broken SQL

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(sql, result.OriginalQuery);
        // Should fallback to regex parser, which may or may not find anything
        // but at least shouldn't crash
    }

    [TestMethod]
    [TestCategory("ScriptDom")]
    public void ParseQuery_EmptyQuery_ReturnsEmptyRequirements()
    {
        // Arrange
        var sql = "";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("", result.OriginalQuery);
        Assert.AreEqual(0, result.TableRequirements.Count);
        Assert.AreEqual(0, result.WhereConditions.Count);
        Assert.AreEqual(0, result.JoinRequirements.Count);
    }

    [TestMethod]
    [TestCategory("ScriptDom")]
    public void ParseQuery_MultipleConditionsWithAnd_ExtractsAllConditions()
    {
        // Arrange
        var sql = @"
            SELECT * FROM users 
            WHERE age > 25 
              AND department = 'IT' 
              AND salary >= 50000 
              AND is_active = 1";

        // Act
        var result = _parser.ParseQuery(sql);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(4, result.WhereConditions.Count);
        
        Assert.IsTrue(result.WhereConditions.Any(w => w.ColumnName == "age" && w.Operator == ">" && w.Value == "25"));
        Assert.IsTrue(result.WhereConditions.Any(w => w.ColumnName == "department" && w.Operator == "=" && w.Value == "IT"));
        Assert.IsTrue(result.WhereConditions.Any(w => w.ColumnName == "salary" && w.Operator == ">=" && w.Value == "50000"));
        Assert.IsTrue(result.WhereConditions.Any(w => w.ColumnName == "is_active" && w.Operator == "=" && w.Value == "1"));
    }
} 