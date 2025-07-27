using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Abstractions;
using System;
using System.Linq;

namespace SqlTestDataGenerator.Tests
{
    /// <summary>
    /// Tests cho DialectHandlerFactory
    /// </summary>
    [TestClass]
    [TestCategory("Factory")]
    public class DialectHandlerFactoryTests
    {
        /// <summary>
        /// Test tạo MySQL dialect handler
        /// </summary>
        [TestMethod]
        public void CreateHandler_MySQL_ShouldReturnMySqlDialectHandler()
        {
            Console.WriteLine("🚀 Testing MySQL dialect handler creation");

            var handler = DialectHandlerFactory.Create(DatabaseType.MySQL);

            Assert.IsNotNull(handler);
            Assert.IsInstanceOfType(handler, typeof(MySqlDialectHandler));
            Assert.AreEqual(DatabaseType.MySQL, handler.SupportedDatabaseType);

            Console.WriteLine("✅ MySQL dialect handler created successfully");
        }

        /// <summary>
        /// Test tạo Oracle dialect handler
        /// </summary>
        [TestMethod]
        public void CreateHandler_Oracle_ShouldReturnOracleDialectHandler()
        {
            Console.WriteLine("🚀 Testing Oracle dialect handler creation");

            var handler = DialectHandlerFactory.Create(DatabaseType.Oracle);

            Assert.IsNotNull(handler);
            Assert.IsInstanceOfType(handler, typeof(OracleDialectHandler));
            Assert.AreEqual(DatabaseType.Oracle, handler.SupportedDatabaseType);

            Console.WriteLine("✅ Oracle dialect handler created successfully");
        }

        /// <summary>
        /// Test tạo handler với database type không hỗ trợ
        /// </summary>
        [TestMethod]
        public void CreateHandler_UnsupportedDatabaseType_ShouldThrowException()
        {
            Console.WriteLine("🚀 Testing unsupported database type");

            Assert.ThrowsException<NotSupportedException>(() =>
            {
                DialectHandlerFactory.Create((DatabaseType)999);
            });

            Console.WriteLine("✅ Exception thrown for unsupported database type");
        }

        /// <summary>
        /// Test lấy danh sách database types được hỗ trợ
        /// </summary>
        [TestMethod]
        public void GetSupportedDatabaseTypes_ShouldReturnAllSupportedTypes()
        {
            Console.WriteLine("🚀 Testing supported database types");

            var supportedTypes = DialectHandlerFactory.GetSupportedDatabaseTypes().ToList();

            Assert.IsNotNull(supportedTypes);
            Assert.IsTrue(supportedTypes.Count > 0);
            Assert.IsTrue(supportedTypes.Contains(DatabaseType.MySQL));
            Assert.IsTrue(supportedTypes.Contains(DatabaseType.Oracle));

            Console.WriteLine($"✅ Found {supportedTypes.Count} supported database types");
            foreach (var type in supportedTypes)
            {
                Console.WriteLine($"  - {type}");
            }
        }

        /// <summary>
        /// Test kiểm tra database type có được hỗ trợ không
        /// </summary>
        [TestMethod]
        public void IsSupported_ShouldReturnCorrectResults()
        {
            Console.WriteLine("🚀 Testing database type support check");

            Assert.IsTrue(DialectHandlerFactory.IsSupported(DatabaseType.MySQL));
            Assert.IsTrue(DialectHandlerFactory.IsSupported(DatabaseType.Oracle));
            Assert.IsFalse(DialectHandlerFactory.IsSupported((DatabaseType)999));

            Console.WriteLine("✅ Database type support check completed");
        }

        /// <summary>
        /// Test tạo và sử dụng MySQL handler
        /// </summary>
        [TestMethod]
        public void CreateAndUseMySqlHandler_ShouldWorkCorrectly()
        {
            Console.WriteLine("🚀 Testing MySQL handler functionality");

            var handler = DialectHandlerFactory.Create(DatabaseType.MySQL);

            // Test identifier escaping
            var escaped = handler.EscapeIdentifier("SELECT");
            Assert.AreEqual("`SELECT`", escaped);

            // Test date function conversion (no change for MySQL)
            var dateFunc = handler.ConvertDateFunction("NOW()");
            Assert.AreEqual("NOW()", dateFunc);

            // Test data type conversion (no change for MySQL)
            var dataType = handler.ConvertDataType("VARCHAR(255)");
            Assert.AreEqual("VARCHAR(255)", dataType);

            // Test value formatting
            var formatted = handler.FormatValue("test", "VARCHAR");
            Assert.AreEqual("'test'", formatted);

            // Test pagination syntax
            var pagination = handler.GetPaginationSyntax(10, 20);
            Assert.AreEqual("LIMIT 20 OFFSET 10", pagination);

            Console.WriteLine("✅ MySQL handler functionality test completed");
        }

        /// <summary>
        /// Test tạo và sử dụng Oracle handler
        /// </summary>
        [TestMethod]
        public void CreateAndUseOracleHandler_ShouldWorkCorrectly()
        {
            Console.WriteLine("🚀 Testing Oracle handler functionality");

            var handler = DialectHandlerFactory.Create(DatabaseType.Oracle);

            // Test identifier escaping
            var escaped = handler.EscapeIdentifier("SELECT");
            Assert.AreEqual("\"SELECT\"", escaped);

            // Test date function conversion
            var dateFunc = handler.ConvertDateFunction("NOW()");
            Assert.AreEqual("SYSDATE", dateFunc);

            // Test data type conversion
            var dataType = handler.ConvertDataType("VARCHAR(255)");
            Assert.AreEqual("VARCHAR2(255)", dataType);

            // Test value formatting
            var formatted = handler.FormatValue("test", "VARCHAR2");
            Assert.AreEqual("'test'", formatted);

            // Test pagination syntax
            var pagination = handler.GetPaginationSyntax(10, 20);
            Assert.AreEqual("WHERE ROWNUM <= 30 AND ROWNUM > 10", pagination);

            Console.WriteLine("✅ Oracle handler functionality test completed");
        }

        /// <summary>
        /// Test so sánh MySQL và Oracle handlers
        /// </summary>
        [TestMethod]
        public void CompareMySqlAndOracleHandlers_ShouldShowDifferences()
        {
            Console.WriteLine("🚀 Testing MySQL vs Oracle handler differences");

            var mysqlHandler = DialectHandlerFactory.Create(DatabaseType.MySQL);
            var oracleHandler = DialectHandlerFactory.Create(DatabaseType.Oracle);

            // Test identifier escaping differences
            var mysqlEscaped = mysqlHandler.EscapeIdentifier("SELECT");
            var oracleEscaped = oracleHandler.EscapeIdentifier("SELECT");
            Assert.AreNotEqual(mysqlEscaped, oracleEscaped);
            Console.WriteLine($"MySQL escaping: {mysqlEscaped}");
            Console.WriteLine($"Oracle escaping: {oracleEscaped}");

            // Test date function conversion differences
            var mysqlDate = mysqlHandler.ConvertDateFunction("NOW()");
            var oracleDate = oracleHandler.ConvertDateFunction("NOW()");
            Assert.AreNotEqual(mysqlDate, oracleDate);
            Console.WriteLine($"MySQL date function: {mysqlDate}");
            Console.WriteLine($"Oracle date function: {oracleDate}");

            // Test data type conversion differences
            var mysqlType = mysqlHandler.ConvertDataType("VARCHAR(255)");
            var oracleType = oracleHandler.ConvertDataType("VARCHAR(255)");
            Assert.AreNotEqual(mysqlType, oracleType);
            Console.WriteLine($"MySQL data type: {mysqlType}");
            Console.WriteLine($"Oracle data type: {oracleType}");

            // Test pagination syntax differences
            var mysqlPagination = mysqlHandler.GetPaginationSyntax(10, 20);
            var oraclePagination = oracleHandler.GetPaginationSyntax(10, 20);
            Assert.AreNotEqual(mysqlPagination, oraclePagination);
            Console.WriteLine($"MySQL pagination: {mysqlPagination}");
            Console.WriteLine($"Oracle pagination: {oraclePagination}");

            Console.WriteLine("✅ Handler differences comparison completed");
        }
    }
} 