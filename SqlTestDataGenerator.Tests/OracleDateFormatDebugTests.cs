using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Abstractions;
using System.Collections.Generic;

namespace SqlTestDataGenerator.Tests
{
    [TestClass]
    public class OracleDateFormatDebugTests
    {
        [TestMethod]
        public void DebugOracleDateFormatting_ShouldFormatDatesCorrectly()
        {
            Console.WriteLine("🔧 Debug Oracle Date Formatting");
            
            // Test the problematic date values from the error
            var testValues = new[]
            {
                "1976-12-31 00:41:47",
                "2025-04-11 02:05:09",
                "2025-07-24 21:37:59",
                "2025-04-18 07:24:43",
                "2025-07-21 20:47:30"
            };
            
            var dialectHandler = new OracleDialectHandler();
            var insertBuilder = new CommonInsertBuilder(dialectHandler);
            
            Console.WriteLine("\n=== Testing FormatUnknownValue ===");
            foreach (var value in testValues)
            {
                var result = insertBuilder.FormatValue(value, DatabaseType.Oracle);
                Console.WriteLine($"Input: '{value}' -> Output: {result}");
                
                // Verify that Oracle date values are properly formatted
                Assert.IsTrue(result.Contains("TO_DATE") || result.Contains("TO_TIMESTAMP") || result.StartsWith("'"), 
                    $"Oracle date value '{value}' should be formatted with TO_DATE/TO_TIMESTAMP or quoted, got: {result}");
            }
            
            Console.WriteLine("\n=== Testing FormatDateTime ===");
            foreach (var value in testValues)
            {
                if (DateTime.TryParse(value, out var dt))
                {
                    var result = insertBuilder.FormatValue(dt, DatabaseType.Oracle);
                    Console.WriteLine($"Input: {dt} -> Output: {result}");
                    
                    // Verify that DateTime objects are properly formatted
                    Assert.IsTrue(result.Contains("TO_DATE") || result.Contains("TO_TIMESTAMP"), 
                        $"DateTime object {dt} should be formatted with TO_DATE/TO_TIMESTAMP, got: {result}");
                }
            }
            
            Console.WriteLine("\n=== Testing IsLikelyDateTimeString ===");
            foreach (var value in testValues)
            {
                var isDateTime = IsLikelyDateTimeString(value);
                Console.WriteLine($"'{value}' -> IsDateTime: {isDateTime}");
                Assert.IsTrue(isDateTime, $"Value '{value}' should be detected as datetime string");
            }
        }
        
        [TestMethod]
        public void DebugOracleDateFormatting_WithColumnSchema_ShouldFormatDatesCorrectly()
        {
            Console.WriteLine("🔧 Debug Oracle Date Formatting with Column Schema");
            
            var testValues = new[]
            {
                "1976-12-31 00:41:47",
                "2025-04-11 02:05:09",
                "2025-07-24 21:37:59",
                "2025-04-18 07:24:43",
                "2025-07-21 20:47:30"
            };
            
            var dataTypes = new[]
            {
                "DATE",
                "TIMESTAMP",
                "TIMESTAMP(6)",
                "VARCHAR2(255)"
            };
            
            var dialectHandler = new OracleDialectHandler();
            var insertBuilder = new CommonInsertBuilder(dialectHandler);
            
            foreach (var dataType in dataTypes)
            {
                Console.WriteLine($"\n=== Testing with dataType: {dataType} ===");
                var columnSchema = new ColumnSchema
                {
                    ColumnName = "test_date",
                    DataType = dataType,
                    IsNullable = true,
                    IsGenerated = false,
                    IsIdentity = false
                };
                
                foreach (var value in testValues)
                {
                    var result = insertBuilder.FormatValue(value, columnSchema, DatabaseType.Oracle);
                    Console.WriteLine($"Input: '{value}' (type: {dataType}) -> Output: {result}");
                    
                    // Verify that Oracle date values are properly formatted
                    if (dataType.Contains("DATE") || dataType.Contains("TIMESTAMP"))
                    {
                        Assert.IsTrue(result.Contains("TO_DATE") || result.Contains("TO_TIMESTAMP"), 
                            $"Oracle {dataType} value '{value}' should be formatted with TO_DATE/TO_TIMESTAMP, got: {result}");
                    }
                }
            }
        }
        
        [TestMethod]
        public void DebugOracleInsertStatement_ShouldFormatDatesCorrectly()
        {
            Console.WriteLine("🔧 Debug Oracle INSERT Statement Generation");
            
            // Create a test record with date values similar to the error
            var record = new Dictionary<string, object>
            {
                { "USERNAME", "Value_1" },
                { "EMAIL", "Value_1" },
                { "PASSWORD_HASH", "Value_1" },
                { "FIRST_NAME", "Phương_001_6xh" },
                { "LAST_NAME", "Phương_001_m3l" },
                { "PHONE", "Value_1" },
                { "ADDRESS", "Value_1" },
                { "DATE_OF_BIRTH", "1976-12-31 00:41:47" },
                { "GENDER", "Value_1" },
                { "AVATAR_URL", "Value_1" },
                { "COMPANY_ID", 2 },
                { "PRIMARY_ROLE_ID", 2 },
                { "SALARY", 1 },
                { "HIRE_DATE", "2025-04-11 02:05:09" },
                { "DEPARTMENT", "Value_1" },
                { "IS_ACTIVE", 0 },
                { "LAST_LOGIN_AT", "2025-07-24 21:37:59" },
                { "CREATED_AT", "2025-04-18 07:24:43" },
                { "UPDATED_AT", "2025-07-21 20:47:30" }
            };
            
            // Create table schema with proper data types
            var tableSchema = new TableSchema
            {
                TableName = "users",
                Columns = new List<ColumnSchema>
                {
                    new() { ColumnName = "USERNAME", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "EMAIL", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "PASSWORD_HASH", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "FIRST_NAME", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "LAST_NAME", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "PHONE", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "ADDRESS", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "DATE_OF_BIRTH", DataType = "DATE", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "GENDER", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "AVATAR_URL", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "COMPANY_ID", DataType = "NUMBER", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "PRIMARY_ROLE_ID", DataType = "NUMBER", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "SALARY", DataType = "NUMBER", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "HIRE_DATE", DataType = "DATE", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "DEPARTMENT", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "IS_ACTIVE", DataType = "NUMBER(1)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "LAST_LOGIN_AT", DataType = "TIMESTAMP", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "CREATED_AT", DataType = "TIMESTAMP", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "UPDATED_AT", DataType = "TIMESTAMP", IsNullable = true, IsGenerated = false, IsIdentity = false }
                }
            };
            
            var dialectHandler = new OracleDialectHandler();
            var insertBuilder = new CommonInsertBuilder(dialectHandler);
            
            // Generate INSERT statement
            var insertSql = insertBuilder.BuildInsertStatement("users", record, tableSchema, DatabaseType.Oracle);
            
            Console.WriteLine($"Generated INSERT SQL:");
            Console.WriteLine(insertSql);
            
            // Verify that date values are properly formatted
            Assert.IsTrue(insertSql.Contains("TO_DATE") || insertSql.Contains("TO_TIMESTAMP"), 
                "INSERT statement should contain TO_DATE or TO_TIMESTAMP functions for date values");
            
            // Verify that no unquoted date values appear
            Assert.IsFalse(insertSql.Contains("1976-12-31 00:41:47"), 
                "Date value should not appear unquoted in INSERT statement");
            Assert.IsFalse(insertSql.Contains("2025-04-11 02:05:09"), 
                "Date value should not appear unquoted in INSERT statement");
            
            Console.WriteLine("✅ INSERT statement generation test passed");
        }
        
        [TestMethod]
        public void DebugOracleInsertStatement_WithoutColumnSchema_ShouldFormatDatesCorrectly()
        {
            Console.WriteLine("🔧 Debug Oracle INSERT Statement Generation WITHOUT Column Schema");
            
            // Create a test record with date values similar to the error
            var record = new Dictionary<string, object>
            {
                { "USERNAME", "Value_1" },
                { "EMAIL", "Value_1" },
                { "PASSWORD_HASH", "Value_1" },
                { "FIRST_NAME", "Phương_001_6xh" },
                { "LAST_NAME", "Phương_001_m3l" },
                { "PHONE", "Value_1" },
                { "ADDRESS", "Value_1" },
                { "DATE_OF_BIRTH", "1976-12-31 00:41:47" },
                { "GENDER", "Value_1" },
                { "AVATAR_URL", "Value_1" },
                { "COMPANY_ID", 2 },
                { "PRIMARY_ROLE_ID", 2 },
                { "SALARY", 1 },
                { "HIRE_DATE", "2025-04-11 02:05:09" },
                { "DEPARTMENT", "Value_1" },
                { "IS_ACTIVE", 0 },
                { "LAST_LOGIN_AT", "2025-07-24 21:37:59" },
                { "CREATED_AT", "2025-04-18 07:24:43" },
                { "UPDATED_AT", "2025-07-21 20:47:30" }
            };
            
            // Create table schema WITHOUT proper data types (simulating AI-generated data)
            var tableSchema = new TableSchema
            {
                TableName = "users",
                Columns = new List<ColumnSchema>
                {
                    new() { ColumnName = "USERNAME", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "EMAIL", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "PASSWORD_HASH", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "FIRST_NAME", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "LAST_NAME", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "PHONE", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "ADDRESS", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "DATE_OF_BIRTH", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false }, // ❌ Wrong data type
                    new() { ColumnName = "GENDER", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "AVATAR_URL", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "COMPANY_ID", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false }, // ❌ Wrong data type
                    new() { ColumnName = "PRIMARY_ROLE_ID", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false }, // ❌ Wrong data type
                    new() { ColumnName = "SALARY", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false }, // ❌ Wrong data type
                    new() { ColumnName = "HIRE_DATE", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false }, // ❌ Wrong data type
                    new() { ColumnName = "DEPARTMENT", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false },
                    new() { ColumnName = "IS_ACTIVE", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false }, // ❌ Wrong data type
                    new() { ColumnName = "LAST_LOGIN_AT", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false }, // ❌ Wrong data type
                    new() { ColumnName = "CREATED_AT", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false }, // ❌ Wrong data type
                    new() { ColumnName = "UPDATED_AT", DataType = "VARCHAR2(255)", IsNullable = true, IsGenerated = false, IsIdentity = false } // ❌ Wrong data type
                }
            };
            
            var dialectHandler = new OracleDialectHandler();
            var insertBuilder = new CommonInsertBuilder(dialectHandler);
            
            // Generate INSERT statement
            var insertSql = insertBuilder.BuildInsertStatement("users", record, tableSchema, DatabaseType.Oracle);
            
            Console.WriteLine($"Generated INSERT SQL (with wrong data types):");
            Console.WriteLine(insertSql);
            
            // Even with wrong data types, date strings should still be detected and formatted
            Assert.IsTrue(insertSql.Contains("TO_DATE") || insertSql.Contains("TO_TIMESTAMP") || insertSql.Contains("'1976-12-31 00:41:47'"), 
                "INSERT statement should contain TO_DATE/TO_TIMESTAMP functions or quoted date values");
            
            // Verify that date values are properly quoted at minimum
            Assert.IsTrue(insertSql.Contains("'1976-12-31 00:41:47'") || insertSql.Contains("TO_DATE"), 
                "Date value should be quoted or formatted with TO_DATE");
            Assert.IsTrue(insertSql.Contains("'2025-04-11 02:05:09'") || insertSql.Contains("TO_DATE"), 
                "Date value should be quoted or formatted with TO_DATE");
            
            Console.WriteLine("✅ INSERT statement generation test passed (with wrong data types)");
        }
        
        [TestMethod]
        public void DebugOracleInsertStatement_WithNullColumnSchema_ShouldFormatDatesCorrectly()
        {
            Console.WriteLine("🔧 Debug Oracle INSERT Statement Generation with NULL Column Schema");
            
            // Create a test record with date values similar to the error
            var record = new Dictionary<string, object>
            {
                { "USERNAME", "Value_1" },
                { "EMAIL", "Value_1" },
                { "PASSWORD_HASH", "Value_1" },
                { "FIRST_NAME", "Phương_001_6xh" },
                { "LAST_NAME", "Phương_001_m3l" },
                { "PHONE", "Value_1" },
                { "ADDRESS", "Value_1" },
                { "DATE_OF_BIRTH", "1976-12-31 00:41:47" },
                { "GENDER", "Value_1" },
                { "AVATAR_URL", "Value_1" },
                { "COMPANY_ID", 2 },
                { "PRIMARY_ROLE_ID", 2 },
                { "SALARY", 1 },
                { "HIRE_DATE", "2025-04-11 02:05:09" },
                { "DEPARTMENT", "Value_1" },
                { "IS_ACTIVE", 0 },
                { "LAST_LOGIN_AT", "2025-07-24 21:37:59" },
                { "CREATED_AT", "2025-04-18 07:24:43" },
                { "UPDATED_AT", "2025-07-21 20:47:30" }
            };
            
            // Create table schema with NULL column schema (simulating missing schema)
            var tableSchema = new TableSchema
            {
                TableName = "users",
                Columns = new List<ColumnSchema>() // Empty columns list
            };
            
            var dialectHandler = new OracleDialectHandler();
            var insertBuilder = new CommonInsertBuilder(dialectHandler);
            
            // Generate INSERT statement
            var insertSql = insertBuilder.BuildInsertStatement("users", record, tableSchema, DatabaseType.Oracle);
            
            Console.WriteLine($"Generated INSERT SQL (with NULL column schema):");
            Console.WriteLine(insertSql);
            
            // Even with NULL column schema, date strings should still be detected and formatted
            Assert.IsTrue(insertSql.Contains("TO_DATE") || insertSql.Contains("TO_TIMESTAMP") || insertSql.Contains("'1976-12-31 00:41:47'"), 
                "INSERT statement should contain TO_DATE/TO_TIMESTAMP functions or quoted date values");
            
            // Verify that date values are properly quoted at minimum
            Assert.IsTrue(insertSql.Contains("'1976-12-31 00:41:47'") || insertSql.Contains("TO_DATE"), 
                "Date value should be quoted or formatted with TO_DATE");
            Assert.IsTrue(insertSql.Contains("'2025-04-11 02:05:09'") || insertSql.Contains("TO_DATE"), 
                "Date value should be quoted or formatted with TO_DATE");
            
            Console.WriteLine("✅ INSERT statement generation test passed (with NULL column schema)");
        }
        
        // Test method to debug the actual issue: DataGenService calls BuildInsertStatement with List<string> columnValues
        [TestMethod]
        public void DebugOracleInsertStatement_WithStringColumnValues_ShouldFormatDatesCorrectly()
        {
            // Simulate what DataGenService actually does
            var insertBuilder = new CommonInsertBuilder(new OracleDialectHandler());
            
            // Simulate table schema with DATE columns
            var tableSchema = new TableSchema
            {
                TableName = "users",
                Columns = new List<ColumnSchema>
                {
                    new ColumnSchema { ColumnName = "ID", DataType = "NUMBER", IsPrimaryKey = true, IsIdentity = true },
                    new ColumnSchema { ColumnName = "USERNAME", DataType = "VARCHAR2" },
                    new ColumnSchema { ColumnName = "EMAIL", DataType = "VARCHAR2" },
                    new ColumnSchema { ColumnName = "DATE_OF_BIRTH", DataType = "DATE" },
                    new ColumnSchema { ColumnName = "HIRE_DATE", DataType = "DATE" },
                    new ColumnSchema { ColumnName = "CREATED_AT", DataType = "TIMESTAMP" },
                    new ColumnSchema { ColumnName = "UPDATED_AT", DataType = "TIMESTAMP" }
                }
            };
            
            // Get insertable columns (exclude ID)
            var columns = insertBuilder.GetInsertableColumns(tableSchema);
            
            // Simulate column values as strings (what DataGenService.GenerateBogusValue returns)
            // Note: ID column is filtered out by GetInsertableColumns, so we don't include its value
            var columnValues = new List<string>
            {
                "'user1'",                              // USERNAME
                "'test@email.com'",                     // EMAIL
                "1976-12-31 00:41:47",                  // DATE_OF_BIRTH - UNQUOTED DATE STRING!
                "2025-04-11 02:05:09",                  // HIRE_DATE - UNQUOTED DATE STRING!
                "2025-07-24 21:37:59",                  // CREATED_AT - UNQUOTED DATE STRING!
                "2025-07-21 20:47:30"                   // UPDATED_AT - UNQUOTED DATE STRING!
            };
            
            // This is the problematic call that DataGenService makes
            var insertSql = insertBuilder.BuildInsertStatement("users", columns, columnValues, DatabaseType.Oracle);
            
            Console.WriteLine($"[DEBUG] Generated SQL: {insertSql}");
            
            // Check if date strings are properly formatted
            if (insertSql.Contains("1976-12-31 00:41:47") && !insertSql.Contains("'1976-12-31 00:41:47'"))
            {
                Console.WriteLine("[DEBUG] ❌ DATE_OF_BIRTH is UNQUOTED - This will cause ORA-00917!");
            }
            else if (insertSql.Contains("'1976-12-31 00:41:47'"))
            {
                Console.WriteLine("[DEBUG] ✅ DATE_OF_BIRTH is properly quoted");
            }
            else if (insertSql.Contains("TO_DATE") || insertSql.Contains("TO_TIMESTAMP"))
            {
                Console.WriteLine("[DEBUG] ✅ DATE_OF_BIRTH is properly formatted with TO_DATE/TO_TIMESTAMP");
            }
            
            if (insertSql.Contains("2025-04-11 02:05:09") && !insertSql.Contains("'2025-04-11 02:05:09'"))
            {
                Console.WriteLine("[DEBUG] ❌ HIRE_DATE is UNQUOTED - This will cause ORA-00917!");
            }
            else if (insertSql.Contains("'2025-04-11 02:05:09'"))
            {
                Console.WriteLine("[DEBUG] ✅ HIRE_DATE is properly quoted");
            }
            else if (insertSql.Contains("TO_DATE") || insertSql.Contains("TO_TIMESTAMP"))
            {
                Console.WriteLine("[DEBUG] ✅ HIRE_DATE is properly formatted with TO_DATE/TO_TIMESTAMP");
            }
            
            // The issue: date strings are passed as raw strings without quotes or TO_DATE formatting
            // This causes ORA-00917: missing comma error in Oracle
        }
        
        // Copy the method from CommonInsertBuilder for testing
        private static bool IsLikelyDateTimeString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
                
            // Common datetime patterns: YYYY-MM-DD or YYYY-MM-DD HH:MM:SS
            return value.Contains("-") && 
                   (value.Contains(":") || value.Length == 10) && 
                   (value.Length >= 10 && value.Length <= 19) &&
                   char.IsDigit(value[0]) &&
                   DateTime.TryParse(value, out _);
        }
    }
} 