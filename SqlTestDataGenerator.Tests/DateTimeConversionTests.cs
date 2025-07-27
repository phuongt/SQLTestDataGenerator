using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using Microsoft.Extensions.Logging;
using System;

namespace SqlTestDataGenerator.Tests
{
    [TestClass]
    public class DateTimeConversionTests
    {
        private CommonInsertBuilder _builder;
        private ILogger<CommonInsertBuilder> _logger;

        [TestInitialize]
        public void Setup()
        {
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CommonInsertBuilder>.Instance;
            var mockHandler = new MySqlDialectHandler();
            _builder = new CommonInsertBuilder(mockHandler);
        }

        [TestMethod]
        public void FormatValue_DateTime_MySQL_ShouldUseSTRToDate()
        {
            // Arrange
            var dateTime = new DateTime(2023, 12, 25, 15, 30, 45);
            
            // Act
            var result = _builder.FormatValue(dateTime, DatabaseType.MySQL);
            
            // Assert
            Assert.AreEqual("STR_TO_DATE('2023-12-25 15:30:45', '%Y-%m-%d %H:%i:%s')", result);
            Console.WriteLine($"MySQL DateTime: {result}");
        }

        [TestMethod]
        public void FormatValue_DateTime_Oracle_ShouldUseToDate()
        {
            // Arrange
            var dateTime = new DateTime(2023, 12, 25, 15, 30, 45);
            
            // Act
            var result = _builder.FormatValue(dateTime, DatabaseType.Oracle);
            
            // Assert
            Assert.AreEqual("TO_DATE('2023-12-25 15:30:45', 'YYYY-MM-DD HH24:MI:SS')", result);
            Console.WriteLine($"Oracle DateTime: {result}");
        }

        [TestMethod]
        public void FormatValue_DateOnly_MySQL_ShouldUseSTRToDate()
        {
            // Arrange
            var dateOnly = new DateOnly(2023, 12, 25);
            
            // Act
            var result = _builder.FormatValue(dateOnly, DatabaseType.MySQL);
            
            // Assert
            Assert.AreEqual("STR_TO_DATE('2023-12-25', '%Y-%m-%d')", result);
            Console.WriteLine($"MySQL DateOnly: {result}");
        }

        [TestMethod]
        public void FormatValue_DateOnly_Oracle_ShouldUseToDate()
        {
            // Arrange
            var dateOnly = new DateOnly(2023, 12, 25);
            
            // Act
            var result = _builder.FormatValue(dateOnly, DatabaseType.Oracle);
            
            // Assert
            Assert.AreEqual("TO_DATE('2023-12-25', 'YYYY-MM-DD')", result);
            Console.WriteLine($"Oracle DateOnly: {result}");
        }

        [TestMethod]
        public void FormatValue_TimeOnly_MySQL_ShouldUseSTRToDate()
        {
            // Arrange
            var timeOnly = new TimeOnly(15, 30, 45);
            
            // Act
            var result = _builder.FormatValue(timeOnly, DatabaseType.MySQL);
            
            // Assert
            Assert.AreEqual("STR_TO_DATE('15:30:45', '%H:%i:%s')", result);
            Console.WriteLine($"MySQL TimeOnly: {result}");
        }

        [TestMethod]
        public void FormatValue_TimeOnly_Oracle_ShouldUseToDate()
        {
            // Arrange
            var timeOnly = new TimeOnly(15, 30, 45);
            
            // Act
            var result = _builder.FormatValue(timeOnly, DatabaseType.Oracle);
            
            // Assert
            Assert.AreEqual("TO_DATE('15:30:45', 'HH24:MI:SS')", result);
            Console.WriteLine($"Oracle TimeOnly: {result}");
        }

        [TestMethod]
        public void FormatValue_DateTimeString_MySQL_ShouldDetectAndConvert()
        {
            // Arrange
            var dateTimeString = "2023-12-25 15:30:45";
            
            // Act
            var result = _builder.FormatValue(dateTimeString, DatabaseType.MySQL);
            
            // Assert
            Assert.IsTrue(result.Contains("STR_TO_DATE"), "Should use STR_TO_DATE for datetime strings");
            Console.WriteLine($"MySQL DateTime String: {result}");
        }

        [TestMethod]
        public void FormatValue_DateTimeString_Oracle_ShouldDetectAndConvert()
        {
            // Arrange
            var dateTimeString = "2023-12-25 15:30:45";
            
            // Act
            var result = _builder.FormatValue(dateTimeString, DatabaseType.Oracle);
            
            // Assert
            Assert.IsTrue(result.Contains("TO_DATE"), "Should use TO_DATE for datetime strings");
            Console.WriteLine($"Oracle DateTime String: {result}");
        }

        [TestMethod]
        public void FormatValue_WithColumnSchema_MySQL_ShouldUseSTRToDate()
        {
            // Arrange
            var columnSchema = new ColumnSchema
            {
                ColumnName = "created_at",
                DataType = "datetime"
            };
            var dateTimeString = "2023-12-25 15:30:45";
            
            // Act
            var result = _builder.FormatValue(dateTimeString, columnSchema, DatabaseType.MySQL);
            
            // Assert
            Assert.IsTrue(result.Contains("STR_TO_DATE"), "Should use STR_TO_DATE with column schema");
            Assert.IsTrue(result.Contains("'2023-12-25 15:30:45'"), "Should contain the date value");
            Console.WriteLine($"MySQL with Schema: {result}");
        }

        [TestMethod]
        public void FormatValue_WithColumnSchema_Oracle_ShouldUseToDate()
        {
            // Arrange  
            var columnSchema = new ColumnSchema
            {
                ColumnName = "created_at",
                DataType = "TIMESTAMP"
            };
            var dateTimeString = "2023-12-25 15:30:45";
            
            // Act
            var result = _builder.FormatValue(dateTimeString, columnSchema, DatabaseType.Oracle);
            
            // Assert
            Assert.IsTrue(result.Contains("TO_DATE"), "Should use TO_DATE with column schema");
            Assert.IsTrue(result.Contains("'2023-12-25 15:30:45'"), "Should contain the date value");
            Console.WriteLine($"Oracle with Schema: {result}");
        }
    }
} 