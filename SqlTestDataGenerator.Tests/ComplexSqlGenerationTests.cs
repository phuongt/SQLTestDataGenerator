using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using System.Data;

namespace SqlTestDataGenerator.Tests;

[TestClass]
public class ComplexSqlGenerationTests
{
    private const string COMPLEX_SQL_QUERY = @"
        -- Tìm user tên Phương, sinh 1989, công ty VNEXT, vai trò DD, sắp nghỉ việc
        SELECT u.id
            ,u.username
            ,u.first_name
            ,u.last_name
            ,u.email
            ,u.date_of_birth
            ,u.salary
            ,u.department
            ,u.hire_date
            ,c.NAME AS company_name
            ,c.code AS company_code
            ,r.NAME AS role_name
            ,r.code AS role_code
            ,ur.expires_at AS role_expires
            ,CASE 
                WHEN u.is_active = 0
                    THEN 'Đã nghỉ việc'
                WHEN ur.expires_at IS NOT NULL
                    AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY)
                    THEN 'Sắp hết hạn vai trò'
                ELSE 'Đang làm việc'
                END AS work_status
        FROM users u
        INNER JOIN companies c ON u.company_id = c.id
        INNER JOIN user_roles ur ON u.id = ur.user_id
            AND ur.is_active = TRUE
        INNER JOIN roles r ON ur.role_id = r.id
        WHERE (
                u.first_name LIKE '%Phương%'
                OR u.last_name LIKE '%Phương%'
                )
            AND YEAR(u.date_of_birth) = 1989
            AND c.NAME LIKE '%VNEXT%'
            AND r.code LIKE '%DD%'
            AND (
                u.is_active = 0
                OR ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY)
                )
        ORDER BY ur.expires_at ASC
            ,u.created_at DESC";

    [TestMethod]
    public void ExtractTablesFromQuery_ComplexSQL_ShouldReturnAllTables()
    {
        // Act
        var tables = SqlMetadataService.ExtractTablesFromQuery(COMPLEX_SQL_QUERY);

        // Assert
        Assert.IsNotNull(tables, "Tables list should not be null");
        Assert.AreEqual(4, tables.Count, "Should extract exactly 4 tables");
        
        CollectionAssert.Contains(tables, "users", "Should contain 'users' table");
        CollectionAssert.Contains(tables, "companies", "Should contain 'companies' table");
        CollectionAssert.Contains(tables, "user_roles", "Should contain 'user_roles' table");
        CollectionAssert.Contains(tables, "roles", "Should contain 'roles' table");

        Console.WriteLine($"Extracted tables: {string.Join(", ", tables)}");
    }

    [TestMethod]
    public void ExtractTablesFromQuery_ComplexSQL_ShouldHandleAliases()
    {
        // Act
        var tables = SqlMetadataService.ExtractTablesFromQuery(COMPLEX_SQL_QUERY);

        // Assert - Table names should not include aliases (u, c, ur, r)
        CollectionAssert.DoesNotContain(tables, "u", "Should not contain alias 'u'");
        CollectionAssert.DoesNotContain(tables, "c", "Should not contain alias 'c'");
        CollectionAssert.DoesNotContain(tables, "ur", "Should not contain alias 'ur'");
        CollectionAssert.DoesNotContain(tables, "r", "Should not contain alias 'r'");
    }

    [TestMethod]
    public void ExtractTablesFromQuery_ComplexSQL_ShouldHandleMultipleJoins()
    {
        // Arrange
        var multiJoinSQL = @"
            SELECT * FROM users u
            INNER JOIN companies c ON u.company_id = c.id
            LEFT JOIN departments d ON u.department_id = d.id
            RIGHT JOIN locations l ON d.location_id = l.id
            FULL OUTER JOIN regions r ON l.region_id = r.id";

        // Act
        var tables = SqlMetadataService.ExtractTablesFromQuery(multiJoinSQL);

        // Assert
        Assert.IsTrue(tables.Count >= 5, "Should extract at least 5 tables from multiple JOINs");
        CollectionAssert.Contains(tables, "users");
        CollectionAssert.Contains(tables, "companies");
        CollectionAssert.Contains(tables, "departments");
        CollectionAssert.Contains(tables, "locations");
        CollectionAssert.Contains(tables, "regions");
    }

    [TestMethod]
    public void ExtractTablesFromQuery_EmptySQL_ShouldReturnEmptyList()
    {
        // Act
        var tables = SqlMetadataService.ExtractTablesFromQuery("");

        // Assert
        Assert.IsNotNull(tables, "Should return empty list, not null");
        Assert.AreEqual(0, tables.Count, "Should return empty list for empty SQL");
    }

    [TestMethod]
    public void ExtractTablesFromQuery_NullSQL_ShouldReturnEmptyList()
    {
        // Act
        var tables = SqlMetadataService.ExtractTablesFromQuery(null!);

        // Assert
        Assert.IsNotNull(tables, "Should return empty list, not null");
        Assert.AreEqual(0, tables.Count, "Should return empty list for null SQL");
    }

    [TestMethod]
    public void ExtractTablesFromQuery_SQLWithComments_ShouldIgnoreComments()
    {
        // Arrange
        var sqlWithComments = @"
            -- This is a comment about finding Phương
            SELECT u.id /* inline comment */, u.name
            FROM users u -- table alias
            /* Multi-line comment
               with table names that should be ignored */
            INNER JOIN companies c ON u.company_id = c.id";

        // Act
        var tables = SqlMetadataService.ExtractTablesFromQuery(sqlWithComments);

        // Assert
        Assert.AreEqual(2, tables.Count, "Should extract 2 tables, ignoring comments");
        CollectionAssert.Contains(tables, "users");
        CollectionAssert.Contains(tables, "companies");
    }

    [TestMethod]
    public void ExtractTablesFromQuery_CaseInsensitive_ShouldWork()
    {
        // Arrange
        var mixedCaseSQL = @"
            select u.id from USERS u
            inner join Companies c on u.company_id = c.id
            LEFT JOIN user_ROLES ur on u.id = ur.user_id";

        // Act
        var tables = SqlMetadataService.ExtractTablesFromQuery(mixedCaseSQL);

        // Assert
        Assert.AreEqual(3, tables.Count, "Should handle mixed case SQL keywords");
        // Note: table names should preserve original case
        Assert.IsTrue(tables.Any(t => t.Equals("USERS", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(tables.Any(t => t.Equals("Companies", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(tables.Any(t => t.Equals("user_ROLES", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ExtractTablesFromQuery_WithSubqueries_ShouldExtractAllTables()
    {
        // Arrange
        var sqlWithSubquery = @"
            SELECT u.id, u.name,
                   (SELECT COUNT(*) FROM orders o WHERE o.user_id = u.id) as order_count
            FROM users u
            WHERE u.company_id IN (
                SELECT c.id FROM companies c WHERE c.name LIKE '%VNEXT%'
            )";

        // Act
        var tables = SqlMetadataService.ExtractTablesFromQuery(sqlWithSubquery);

        // Assert
        Assert.IsTrue(tables.Count >= 3, "Should extract tables from main query and subqueries");
        CollectionAssert.Contains(tables, "users");
        CollectionAssert.Contains(tables, "orders");  
        CollectionAssert.Contains(tables, "companies");
    }

    [TestMethod]
    public void ExtractTablesFromQuery_ComplexSQL_ShouldNotContainReservedWords()
    {
        // Act
        var tables = SqlMetadataService.ExtractTablesFromQuery(COMPLEX_SQL_QUERY);

        // Assert - Should not contain SQL reserved words
        var reservedWords = new[] { "SELECT", "FROM", "WHERE", "JOIN", "INNER", "AND", "OR", "LIKE", "CASE", "WHEN", "THEN", "ELSE", "END", "ORDER", "BY" };
        
        foreach (var reserved in reservedWords)
        {
            CollectionAssert.DoesNotContain(tables, reserved, $"Should not contain reserved word '{reserved}'");
        }
    }

    [TestMethod]
    public void ExtractTablesFromQuery_MalformedSQL_ShouldHandleGracefully()
    {
        // Arrange
        var malformedSQL = @"
            SELECT u.id FROM users u
            INNE JOIN companies -- missing R in INNER
            LEFT JOI roles r ON -- missing N in JOIN
            WHERE u.id = ";

        // Act & Assert - Should either return partial results or throw meaningful exception
        try
        {
            var tables = SqlMetadataService.ExtractTablesFromQuery(malformedSQL);
            // If it succeeds, should at least get 'users'
            Assert.IsTrue(tables.Count >= 1, "Should extract at least the valid table");
            CollectionAssert.Contains(tables, "users");
        }
        catch (InvalidOperationException ex)
        {
            // If it throws, should have meaningful error message
            Assert.IsTrue(ex.Message.Contains("SQL"), "Error message should mention SQL");
            Console.WriteLine($"Expected exception for malformed SQL: {ex.Message}");
        }
    }
} 