using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Unit tests tập trung vào fix duplicate key issue trong user_roles table
/// Reproduce lỗi: Duplicate entry 'X-Y' for key 'user_roles.unique_user_role'
/// </summary>
[TestClass]
[TestCategory("BugFix")]
public class DuplicateKeyBugFixTests
{
    private DataGenService _dataGenService = null!;

    [TestInitialize]
    public void Setup()
    {
        _dataGenService = new DataGenService();
    }

    [TestMethod]
    public void GenerateBogusData_UserRoles_ShouldNotHaveDuplicateKeys()
    {
        // Arrange - Create minimal database info focusing on user_roles issue
        var databaseInfo = new DatabaseInfo
        {
            Type = DatabaseType.MySQL,
            Tables = new Dictionary<string, TableSchema>()
        };

        // Add companies table
        databaseInfo.Tables["companies"] = new TableSchema
        {
            TableName = "companies",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "name", DataType = "varchar", MaxLength = 100 },
                new() { ColumnName = "code", DataType = "varchar", MaxLength = 20 }
            },
            ForeignKeys = new List<ForeignKeySchema>()
        };

        // Add roles table
        databaseInfo.Tables["roles"] = new TableSchema
        {
            TableName = "roles",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "name", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "code", DataType = "varchar", MaxLength = 10 }
            },
            ForeignKeys = new List<ForeignKeySchema>()
        };

        // Add users table
        databaseInfo.Tables["users"] = new TableSchema
        {
            TableName = "users",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "username", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "email", DataType = "varchar", MaxLength = 100 },
                new() { ColumnName = "company_id", DataType = "int" }
            },
            ForeignKeys = new List<ForeignKeySchema>
            {
                new() { ColumnName = "company_id", ReferencedTable = "companies", ReferencedColumn = "id" }
            }
        };

        // Add user_roles table - the problematic one
        databaseInfo.Tables["user_roles"] = new TableSchema
        {
            TableName = "user_roles",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "user_id", DataType = "int", IsPrimaryKey = true },
                new() { ColumnName = "role_id", DataType = "int", IsPrimaryKey = true },
                new() { ColumnName = "assigned_by", DataType = "int" },
                new() { ColumnName = "assigned_at", DataType = "datetime" },
                new() { ColumnName = "expires_at", DataType = "datetime" },
                new() { ColumnName = "is_active", DataType = "int" }
            },
            ForeignKeys = new List<ForeignKeySchema>
            {
                new() { ColumnName = "user_id", ReferencedTable = "users", ReferencedColumn = "id" },
                new() { ColumnName = "role_id", ReferencedTable = "roles", ReferencedColumn = "id" },
                new() { ColumnName = "assigned_by", ReferencedTable = "users", ReferencedColumn = "id" }
            }
        };

        // Act - Generate data for 10 records
        var recordCount = 10;
        var result = _dataGenService.GenerateBogusData(databaseInfo, recordCount);

        // Assert - Check for duplicates in user_roles
        var userRoleStatements = result.Where(s => s.TableName.ToLower() == "user_roles").ToList();
        
        Console.WriteLine($"Generated {userRoleStatements.Count} user_roles INSERT statements:");
        
        var usedCombinations = new HashSet<string>();
        var duplicateFound = false;
        var duplicateCombination = "";

        foreach (var statement in userRoleStatements)
        {
            var combination = ExtractUserRoleCombination(statement.SqlStatement);
            Console.WriteLine($"  {combination} -> {statement.SqlStatement}");
            
            if (usedCombinations.Contains(combination))
            {
                duplicateFound = true;
                duplicateCombination = combination;
                break;
            }
            usedCombinations.Add(combination);
        }

        // Main assertion - no duplicates should exist
        Assert.IsFalse(duplicateFound, 
            $"❌ DUPLICATE KEY FOUND: {duplicateCombination} appears multiple times in user_roles!");

        // Additional checks
        Assert.AreEqual(recordCount, userRoleStatements.Count, 
            "Should generate exactly one user_role per record");
        
        Assert.IsTrue(userRoleStatements.All(s => s.SqlStatement.Contains("INSERT INTO")), 
            "All statements should be INSERT statements");

        Console.WriteLine("✅ SUCCESS: No duplicate keys found in user_roles table");
    }

    [TestMethod]
    public void GenerateBogusData_MultipleRuns_ShouldAlwaysProduceUniqueKeys()
    {
        // Arrange - Same setup as above but test multiple runs
        var databaseInfo = CreateTestDatabaseInfo();
        var recordCount = 8; // Use same count as failing test

        for (int run = 1; run <= 5; run++)
        {
            Console.WriteLine($"\n=== RUN {run} ===");
            
            // Act
            var result = _dataGenService.GenerateBogusData(databaseInfo, recordCount);
            var userRoleStatements = result.Where(s => s.TableName.ToLower() == "user_roles").ToList();

            // Assert
            var combinations = userRoleStatements
                .Select(s => ExtractUserRoleCombination(s.SqlStatement))
                .ToList();

            var uniqueCombinations = combinations.Distinct().ToList();
            
            Console.WriteLine($"Generated combinations: {string.Join(", ", combinations)}");
            
            Assert.AreEqual(combinations.Count, uniqueCombinations.Count,
                $"Run {run}: Found duplicate combinations! Generated: [{string.Join(", ", combinations)}]");
        }
        
        Console.WriteLine("✅ SUCCESS: All 5 runs produced unique combinations");
    }

    [TestMethod]
    public void GenerateBogusData_LargeDataset_ShouldHandleUniqueConstraints()
    {
        // Arrange - Test with larger dataset to stress-test uniqueness
        var databaseInfo = CreateTestDatabaseInfo();
        var recordCount = 50; // Larger dataset

        // Act
        var result = _dataGenService.GenerateBogusData(databaseInfo, recordCount);
        var userRoleStatements = result.Where(s => s.TableName.ToLower() == "user_roles").ToList();

        // Assert
        var combinations = userRoleStatements
            .Select(s => ExtractUserRoleCombination(s.SqlStatement))
            .ToList();

        var uniqueCombinations = combinations.Distinct().ToList();
        
        Console.WriteLine($"Generated {combinations.Count} combinations, {uniqueCombinations.Count} unique");
        Console.WriteLine($"Sample combinations: {string.Join(", ", combinations.Take(10))}");
        
        Assert.AreEqual(combinations.Count, uniqueCombinations.Count,
            $"Found {combinations.Count - uniqueCombinations.Count} duplicate combinations in large dataset!");
            
        Assert.AreEqual(recordCount, userRoleStatements.Count,
            "Should generate exactly one user_role per record even in large dataset");

        Console.WriteLine("✅ SUCCESS: Large dataset handled without duplicate keys");
    }

    [TestMethod]
    public void GenerateBogusData_EdgeCase_SmallRecordCount_ShouldWork()
    {
        // Arrange - Test edge case with very small record count
        var databaseInfo = CreateTestDatabaseInfo();
        
        // Test different small record counts
        foreach (int recordCount in new[] { 1, 2, 3, 5 })
        {
            Console.WriteLine($"\n=== Testing {recordCount} records ===");
            
            // Act
            var result = _dataGenService.GenerateBogusData(databaseInfo, recordCount);
            var userRoleStatements = result.Where(s => s.TableName.ToLower() == "user_roles").ToList();

            // Assert
            Assert.AreEqual(recordCount, userRoleStatements.Count,
                $"Should generate exactly {recordCount} user_role statements");

            var combinations = userRoleStatements
                .Select(s => ExtractUserRoleCombination(s.SqlStatement))
                .ToList();

            var uniqueCombinations = combinations.Distinct().ToList();
            
            Console.WriteLine($"Combinations: {string.Join(", ", combinations)}");
            
            Assert.AreEqual(combinations.Count, uniqueCombinations.Count,
                $"Found duplicates with {recordCount} records: {string.Join(", ", combinations)}");
        }
        
        Console.WriteLine("✅ SUCCESS: All edge cases handled correctly");
    }

    /// <summary>
    /// Helper method to create test database info
    /// </summary>
    private static DatabaseInfo CreateTestDatabaseInfo()
    {
        var databaseInfo = new DatabaseInfo
        {
            Type = DatabaseType.MySQL,
            Tables = new Dictionary<string, TableSchema>()
        };

        // Add all required tables with proper foreign key relationships
        databaseInfo.Tables["companies"] = new TableSchema
        {
            TableName = "companies",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "name", DataType = "varchar", MaxLength = 100 }
            },
            ForeignKeys = new List<ForeignKeySchema>()
        };

        databaseInfo.Tables["roles"] = new TableSchema
        {
            TableName = "roles",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "name", DataType = "varchar", MaxLength = 50 }
            },
            ForeignKeys = new List<ForeignKeySchema>()
        };

        databaseInfo.Tables["users"] = new TableSchema
        {
            TableName = "users",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "id", DataType = "int", IsPrimaryKey = true, IsIdentity = true },
                new() { ColumnName = "username", DataType = "varchar", MaxLength = 50 },
                new() { ColumnName = "company_id", DataType = "int" }
            },
            ForeignKeys = new List<ForeignKeySchema>
            {
                new() { ColumnName = "company_id", ReferencedTable = "companies", ReferencedColumn = "id" }
            }
        };

        databaseInfo.Tables["user_roles"] = new TableSchema
        {
            TableName = "user_roles",
            Columns = new List<ColumnSchema>
            {
                new() { ColumnName = "user_id", DataType = "int", IsPrimaryKey = true },
                new() { ColumnName = "role_id", DataType = "int", IsPrimaryKey = true },
                new() { ColumnName = "assigned_by", DataType = "int" },
                new() { ColumnName = "is_active", DataType = "int" }
            },
            ForeignKeys = new List<ForeignKeySchema>
            {
                new() { ColumnName = "user_id", ReferencedTable = "users", ReferencedColumn = "id" },
                new() { ColumnName = "role_id", ReferencedTable = "roles", ReferencedColumn = "id" },
                new() { ColumnName = "assigned_by", ReferencedTable = "users", ReferencedColumn = "id" }
            }
        };

        return databaseInfo;
    }

    /// <summary>
    /// Extract user_id-role_id combination from INSERT statement
    /// </summary>
    private static string ExtractUserRoleCombination(string insertSQL)
    {
        // Parse: INSERT INTO `user_roles` (...) VALUES (user_id, role_id, ...)
        // Expected format: VALUES (1, 2, ...)
        var valuesMatch = System.Text.RegularExpressions.Regex.Match(
            insertSQL, 
            @"VALUES\s*\((\d+),\s*(\d+)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (valuesMatch.Success)
        {
            var userId = valuesMatch.Groups[1].Value;
            var roleId = valuesMatch.Groups[2].Value;
            return $"{userId}-{roleId}";
        }

        return "unknown";
    }
} 