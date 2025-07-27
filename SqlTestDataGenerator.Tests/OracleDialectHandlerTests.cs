using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Abstractions;
using SqlTestDataGenerator.Core.Services;
using System;

namespace SqlTestDataGenerator.Tests
{
    /// <summary>
    /// Tests cho Oracle Dialect Handler
    /// </summary>
    [TestClass]
    [TestCategory("Oracle")]
    [TestCategory("Dialect")]
    public class OracleDialectHandlerTests
    {
        private ISqlDialectHandler _dialectHandler = null!;

        [TestInitialize]
        public void Setup()
        {
            Console.WriteLine("=== Oracle Dialect Handler Test Setup ===");
            _dialectHandler = new OracleDialectHandler();
        }

        [TestCleanup]
        public void Cleanup()
        {
            Console.WriteLine("=== Oracle Dialect Handler Test Cleanup ===");
            _dialectHandler = null;
        }

        /// <summary>
        /// Test Oracle identifier escaping
        /// </summary>
        [TestMethod]
        public void EscapeIdentifier_ShouldEscapeCorrectly()
        {
            Console.WriteLine("🚀 Testing Oracle identifier escaping");

            var testCases = new[]
            {
                new { Input = "users", Expected = "users" },
                new { Input = "USER_ROLES", Expected = "USER_ROLES" },
                new { Input = "MyTable", Expected = "\"MyTable\"" },
                new { Input = "table-name", Expected = "\"table-name\"" },
                new { Input = "table name", Expected = "\"table name\"" },
                new { Input = "SELECT", Expected = "\"SELECT\"" }, // Reserved word
                new { Input = "ORDER", Expected = "\"ORDER\"" }   // Reserved word
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"Testing: {testCase.Input} -> {testCase.Expected}");
                var result = _dialectHandler.EscapeIdentifier(testCase.Input);
                Assert.AreEqual(testCase.Expected, result);
            }

            Console.WriteLine("✅ Oracle identifier escaping tests completed");
        }

        /// <summary>
        /// Test Oracle supported database type
        /// </summary>
        [TestMethod]
        public void SupportedDatabaseType_ShouldBeOracle()
        {
            Console.WriteLine("🚀 Testing Oracle supported database type");

            var databaseType = _dialectHandler.SupportedDatabaseType;
            Assert.AreEqual(DatabaseType.Oracle, databaseType);

            Console.WriteLine("✅ Oracle supported database type test completed");
        }

        /// <summary>
        /// Test Oracle date function conversion
        /// </summary>
        [TestMethod]
        public void ConvertDateFunction_ShouldConvertCorrectly()
        {
            Console.WriteLine("🚀 Testing Oracle date function conversion");

            var testCases = new[]
            {
                new { Input = "NOW()", Expected = "SYSDATE" },
                new { Input = "CURDATE()", Expected = "TRUNC(SYSDATE)" },
                new { Input = "CURTIME()", Expected = "TO_CHAR(SYSDATE, 'HH24:MI:SS')" },
                new { Input = "DATE_ADD(NOW(), INTERVAL 30 DAY)", Expected = "SYSDATE + 30" },
                new { Input = "DATE_SUB(NOW(), INTERVAL 7 DAY)", Expected = "SYSDATE - 7" },
                new { Input = "YEAR(date_column)", Expected = "EXTRACT(YEAR FROM date_column)" },
                new { Input = "MONTH(date_column)", Expected = "EXTRACT(MONTH FROM date_column)" },
                new { Input = "DAY(date_column)", Expected = "EXTRACT(DAY FROM date_column)" }
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"Testing: {testCase.Input} -> {testCase.Expected}");
                var result = _dialectHandler.ConvertDateFunction(testCase.Input);
                Assert.AreEqual(testCase.Expected, result);
            }

            Console.WriteLine("✅ Oracle date function conversion tests completed");
        }

        /// <summary>
        /// Test Oracle data type conversion
        /// </summary>
        [TestMethod]
        public void ConvertDataType_ShouldConvertCorrectly()
        {
            Console.WriteLine("🚀 Testing Oracle data type conversion");

            var testCases = new[]
            {
                new { Input = "INT", Expected = "NUMBER" },
                new { Input = "BIGINT", Expected = "NUMBER" },
                new { Input = "VARCHAR(255)", Expected = "VARCHAR2(255)" },
                new { Input = "TEXT", Expected = "CLOB" },
                new { Input = "DATETIME", Expected = "TIMESTAMP" },
                new { Input = "BOOLEAN", Expected = "NUMBER(1)" },
                new { Input = "JSON", Expected = "CLOB" }
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"Testing: {testCase.Input} -> {testCase.Expected}");
                var result = _dialectHandler.ConvertDataType(testCase.Input);
                Assert.AreEqual(testCase.Expected, result);
            }

            Console.WriteLine("✅ Oracle data type conversion tests completed");
        }

        /// <summary>
        /// Test Oracle value formatting
        /// </summary>
        [TestMethod]
        public void FormatValue_ShouldFormatCorrectly()
        {
            Console.WriteLine("🚀 Testing Oracle value formatting");

            var testCases = new[]
            {
                new { Value = "test", DataType = "VARCHAR2", Expected = "'test'" },
                new { Value = "2023-12-25", DataType = "DATE", Expected = "TO_DATE('2023-12-25', 'YYYY-MM-DD')" },
                new { Value = "2023-12-25 10:30:00", DataType = "TIMESTAMP", Expected = "TO_TIMESTAMP('2023-12-25 10:30:00', 'YYYY-MM-DD HH24:MI:SS')" },
                new { Value = "123", DataType = "NUMBER", Expected = "123" },
                new { Value = "true", DataType = "NUMBER(1)", Expected = "1" },
                new { Value = "false", DataType = "NUMBER(1)", Expected = "0" },
                new { Value = "null", DataType = "VARCHAR2", Expected = "NULL" }
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"Testing: {testCase.Value} ({testCase.DataType}) -> {testCase.Expected}");
                var result = _dialectHandler.FormatValue(testCase.Value, testCase.DataType);
                Assert.AreEqual(testCase.Expected, result);
            }

            Console.WriteLine("✅ Oracle value formatting tests completed");
        }

        /// <summary>
        /// Test Oracle SQL statement termination
        /// </summary>
        [TestMethod]
        public void GetStatementTerminator_ShouldReturnCorrectTerminator()
        {
            Console.WriteLine("🚀 Testing Oracle SQL statement termination");

            var terminator = _dialectHandler.GetStatementTerminator();
            Assert.AreEqual("", terminator); // Oracle doesn't use semicolons in ExecuteNonQueryAsync

            Console.WriteLine("✅ Oracle SQL statement termination test completed");
        }

        /// <summary>
        /// Test Oracle pagination syntax
        /// </summary>
        [TestMethod]
        public void GetPaginationSyntax_ShouldReturnCorrectSyntax()
        {
            Console.WriteLine("🚀 Testing Oracle pagination syntax");

            var testCases = new[]
            {
                new { Offset = 0, Limit = 10, Expected = "WHERE ROWNUM <= 10" },
                new { Offset = 10, Limit = 10, Expected = "WHERE ROWNUM <= 20 AND ROWNUM > 10" }
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"Testing: OFFSET {testCase.Offset}, LIMIT {testCase.Limit} -> {testCase.Expected}");
                var result = _dialectHandler.GetPaginationSyntax(testCase.Offset, testCase.Limit);
                Assert.AreEqual(testCase.Expected, result);
            }

            Console.WriteLine("✅ Oracle pagination syntax tests completed");
        }

        /// <summary>
        /// Test Oracle auto-increment syntax
        /// </summary>
        [TestMethod]
        public void GetAutoIncrementSyntax_ShouldReturnCorrectSyntax()
        {
            Console.WriteLine("🚀 Testing Oracle auto-increment syntax");

            var testCases = new[]
            {
                new { TableName = "users", ColumnName = "id", Expected = "users_seq.NEXTVAL" },
                new { TableName = "orders", ColumnName = "order_id", Expected = "orders_seq.NEXTVAL" }
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"Testing: {testCase.TableName}.{testCase.ColumnName} -> {testCase.Expected}");
                var result = _dialectHandler.GetAutoIncrementSyntax(testCase.TableName, testCase.ColumnName);
                Assert.AreEqual(testCase.Expected, result);
            }

            Console.WriteLine("✅ Oracle auto-increment syntax tests completed");
        }
    }
} 