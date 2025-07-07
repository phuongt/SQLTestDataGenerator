using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqlTestDataGenerator.Tests;

[TestClass]
public class DataGeneratorTests
{
    private Mock<ILoggerService> _mockLogger;
    private Mock<ISqlTemplateParser> _mockTemplateParser;
    private Mock<IDbExecutor> _mockDbExecutor;
    private DataGenerator _dataGenerator;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILoggerService>();
        _mockTemplateParser = new Mock<ISqlTemplateParser>();
        _mockDbExecutor = new Mock<IDbExecutor>();
        _dataGenerator = new DataGenerator(_mockLogger.Object, _mockTemplateParser.Object, _mockDbExecutor.Object);
    }

    [TestMethod]
    public void GenerateData_ValidTemplate_ShouldReturnSuccess()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Name = "Test Template",
            Template = "INSERT INTO users (name) VALUES ('{RandomName}')"
        };

        var settings = new GenerationSettings
        {
            RecordCount = 5,
            DatabaseType = "MySQL",
            ConnectionString = "test-connection"
        };

        var validationResult = new TemplateValidationResult { IsValid = true };
        var generatedSql = new List<string>
        {
            "INSERT INTO users (name) VALUES ('John Doe')",
            "INSERT INTO users (name) VALUES ('Jane Smith')",
            "INSERT INTO users (name) VALUES ('Bob Johnson')",
            "INSERT INTO users (name) VALUES ('Alice Brown')",
            "INSERT INTO users (name) VALUES ('Charlie Wilson')"
        };

        var executionResult = new ExecutionResult
        {
            Success = true,
            RecordsGenerated = 5,
            ResultData = new System.Data.DataTable()
        };

        _mockTemplateParser.Setup(x => x.ValidateTemplate(template)).Returns(validationResult);
        _mockTemplateParser.Setup(x => x.GenerateMultiple(template, settings, settings.RecordCount)).Returns(generatedSql);
        _mockDbExecutor.Setup(x => x.ExecuteStatements(generatedSql, settings)).Returns(executionResult);

        // Act
        var result = _dataGenerator.GenerateData(template, settings);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(5, result.StatementsExecuted);
        Assert.AreEqual(5, result.RecordsGenerated);
        Assert.AreEqual(generatedSql.Count, result.GeneratedSql.Count);

        // Verify method calls
        _mockTemplateParser.Verify(x => x.ValidateTemplate(template), Times.Once);
        _mockTemplateParser.Verify(x => x.GenerateMultiple(template, settings, settings.RecordCount), Times.Once);
        _mockDbExecutor.Verify(x => x.ExecuteStatements(generatedSql, settings), Times.Once);

        // Verify logging
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DataGenerator.GenerateData), It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.LogMethodExit(nameof(DataGenerator.GenerateData), It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    public void GenerateData_NullTemplate_ShouldThrowException()
    {
        // Arrange
        SqlTemplate template = null;
        var settings = new GenerationSettings();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => _dataGenerator.GenerateData(template, settings));
    }

    [TestMethod]
    public void GenerateData_NullSettings_ShouldThrowException()
    {
        // Arrange
        var template = new SqlTemplate();
        GenerationSettings settings = null;

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => _dataGenerator.GenerateData(template, settings));
    }

    [TestMethod]
    public void GenerateData_InvalidSettings_ShouldReturnFailure()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Template = "INSERT INTO users (name) VALUES ('{RandomName}')"
        };

        var settings = new GenerationSettings
        {
            RecordCount = -1, // Invalid record count
            DatabaseType = "MySQL"
        };

        var validationResult = new ValidationResult
        {
            IsValid = false,
            Messages = new List<ValidationMessage>
            {
                new ValidationMessage { Severity = ValidationSeverity.Error, Message = "Record count must be greater than 0" }
            }
        };

        // Mock ValidateSettings to return invalid result
        // Note: We need to set up the actual ValidateSettings call since it's called internally

        // Act
        var result = _dataGenerator.GenerateData(template, settings);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid generation settings", result.ErrorMessage);
        Assert.IsTrue(result.ValidationMessages.Any());
    }

    [TestMethod]
    public void GenerateData_InvalidTemplate_ShouldReturnFailure()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Template = "" // Empty template
        };

        var settings = new GenerationSettings
        {
            RecordCount = 5,
            DatabaseType = "MySQL"
        };

        var templateValidation = new TemplateValidationResult
        {
            IsValid = false,
            Messages = new List<ValidationMessage>
            {
                new ValidationMessage { Severity = ValidationSeverity.Error, Message = "Template cannot be empty" }
            }
        };

        _mockTemplateParser.Setup(x => x.ValidateTemplate(template)).Returns(templateValidation);

        // Act
        var result = _dataGenerator.GenerateData(template, settings);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid template", result.ErrorMessage);
        Assert.IsTrue(result.ValidationMessages.Any());
    }

    [TestMethod]
    public void GenerateData_NoConnectionString_ShouldGenerateWithoutExecution()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Template = "INSERT INTO users (name) VALUES ('{RandomName}')"
        };

        var settings = new GenerationSettings
        {
            RecordCount = 3,
            DatabaseType = "MySQL",
            ConnectionString = "" // No connection string
        };

        var validationResult = new TemplateValidationResult { IsValid = true };
        var generatedSql = new List<string>
        {
            "INSERT INTO users (name) VALUES ('John Doe')",
            "INSERT INTO users (name) VALUES ('Jane Smith')",
            "INSERT INTO users (name) VALUES ('Bob Johnson')"
        };

        _mockTemplateParser.Setup(x => x.ValidateTemplate(template)).Returns(validationResult);
        _mockTemplateParser.Setup(x => x.GenerateMultiple(template, settings, settings.RecordCount)).Returns(generatedSql);

        // Act
        var result = _dataGenerator.GenerateData(template, settings);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(3, result.StatementsExecuted);
        Assert.AreEqual(3, result.RecordsGenerated);
        Assert.AreEqual(generatedSql.Count, result.GeneratedSql.Count);

        // Verify DbExecutor was not called
        _mockDbExecutor.Verify(x => x.ExecuteStatements(It.IsAny<List<string>>(), It.IsAny<GenerationSettings>()), Times.Never);
    }

    [TestMethod]
    public void GenerateData_ExecutionFailure_ShouldReturnFailure()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Template = "INSERT INTO users (name) VALUES ('{RandomName}')"
        };

        var settings = new GenerationSettings
        {
            RecordCount = 2,
            DatabaseType = "MySQL",
            ConnectionString = "test-connection"
        };

        var validationResult = new TemplateValidationResult { IsValid = true };
        var generatedSql = new List<string>
        {
            "INSERT INTO users (name) VALUES ('John Doe')",
            "INSERT INTO users (name) VALUES ('Jane Smith')"
        };

        var executionResult = new ExecutionResult
        {
            Success = false,
            ErrorMessage = "Database connection failed",
            Exception = new InvalidOperationException("Connection timeout")
        };

        _mockTemplateParser.Setup(x => x.ValidateTemplate(template)).Returns(validationResult);
        _mockTemplateParser.Setup(x => x.GenerateMultiple(template, settings, settings.RecordCount)).Returns(generatedSql);
        _mockDbExecutor.Setup(x => x.ExecuteStatements(generatedSql, settings)).Returns(executionResult);

        // Act
        var result = _dataGenerator.GenerateData(template, settings);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Database connection failed", result.ErrorMessage);
        Assert.IsNotNull(result.Exception);
    }

    [TestMethod]
    public async Task GenerateDataAsync_ValidTemplate_ShouldReturnSuccess()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Template = "INSERT INTO users (name) VALUES ('{RandomName}')"
        };

        var settings = new GenerationSettings
        {
            RecordCount = 2,
            DatabaseType = "MySQL"
        };

        var validationResult = new TemplateValidationResult { IsValid = true };
        var generatedSql = new List<string>
        {
            "INSERT INTO users (name) VALUES ('John Doe')",
            "INSERT INTO users (name) VALUES ('Jane Smith')"
        };

        _mockTemplateParser.Setup(x => x.ValidateTemplate(template)).Returns(validationResult);
        _mockTemplateParser.Setup(x => x.GenerateMultiple(template, settings, settings.RecordCount)).Returns(generatedSql);

        // Act
        var result = await _dataGenerator.GenerateDataAsync(template, settings);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.RecordsGenerated);

        // Verify logging for async method
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DataGenerator.GenerateDataAsync), It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.LogMethodExit(nameof(DataGenerator.GenerateDataAsync), It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    public async Task GenerateMultipleAsync_ValidTemplates_ShouldReturnAllResults()
    {
        // Arrange
        var templates = new List<SqlTemplate>
        {
            new SqlTemplate { Id = "template1", Template = "INSERT INTO users (name) VALUES ('{RandomName}')" },
            new SqlTemplate { Id = "template2", Template = "INSERT INTO products (name) VALUES ('{RandomString}')" }
        };

        var settings = new GenerationSettings
        {
            RecordCount = 1,
            DatabaseType = "MySQL",
            Performance = new PerformanceSettings { MaxParallelOperations = 2 }
        };

        var validationResult = new TemplateValidationResult { IsValid = true };
        var generatedSql = new List<string> { "INSERT INTO test VALUES ('data')" };

        _mockTemplateParser.Setup(x => x.ValidateTemplate(It.IsAny<SqlTemplate>())).Returns(validationResult);
        _mockTemplateParser.Setup(x => x.GenerateMultiple(It.IsAny<SqlTemplate>(), settings, settings.RecordCount)).Returns(generatedSql);

        // Act
        var results = await _dataGenerator.GenerateMultipleAsync(templates, settings);

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(r => r.Success));

        // Verify logging
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DataGenerator.GenerateMultipleAsync), It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.LogMethodExit(nameof(DataGenerator.GenerateMultipleAsync), It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    public void PreviewData_ValidTemplate_ShouldReturnLimitedResults()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Template = "INSERT INTO users (name) VALUES ('{RandomName}')"
        };

        var settings = new GenerationSettings
        {
            RecordCount = 20 // Large number
        };

        var generatedSql = new List<string>();
        for (int i = 0; i < 10; i++) // Should be limited to 10
        {
            generatedSql.Add($"INSERT INTO users (name) VALUES ('User{i}')");
        }

        _mockTemplateParser.Setup(x => x.GenerateMultiple(It.IsAny<SqlTemplate>(), It.IsAny<GenerationSettings>(), 10))
                          .Returns(generatedSql);

        // Act
        var result = _dataGenerator.PreviewData(template, settings);

        // Assert
        Assert.AreEqual(10, result.Count);
        Assert.IsTrue(result.All(sql => sql.StartsWith("INSERT INTO users")));

        // Verify preview was limited to 10 records
        _mockTemplateParser.Verify(x => x.GenerateMultiple(It.IsAny<SqlTemplate>(), It.IsAny<GenerationSettings>(), 10), Times.Once);
    }

    [TestMethod]
    public void ValidateSettings_ValidSettings_ShouldReturnValid()
    {
        // Arrange
        var settings = new GenerationSettings
        {
            RecordCount = 10,
            DatabaseType = "MySQL",
            UseAI = false,
            Performance = new PerformanceSettings
            {
                MaxParallelOperations = 4,
                MemoryLimitMB = 256
            }
        };

        // Act
        var result = _dataGenerator.ValidateSettings(settings);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Messages.Count(m => m.Severity == ValidationSeverity.Error));
    }

    [TestMethod]
    public void ValidateSettings_NullSettings_ShouldReturnInvalid()
    {
        // Arrange
        GenerationSettings settings = null;

        // Act
        var result = _dataGenerator.ValidateSettings(settings);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Messages.Any(m => m.Message.Contains("cannot be null")));
    }

    [TestMethod]
    public void ValidateSettings_InvalidRecordCount_ShouldReturnInvalid()
    {
        // Arrange
        var settings = new GenerationSettings
        {
            RecordCount = 0, // Invalid
            DatabaseType = "MySQL"
        };

        // Act
        var result = _dataGenerator.ValidateSettings(settings);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Messages.Any(m => m.Message.Contains("must be greater than 0")));
    }

    [TestMethod]
    public void ValidateSettings_UnsupportedDatabaseType_ShouldReturnInvalid()
    {
        // Arrange
        var settings = new GenerationSettings
        {
            RecordCount = 5,
            DatabaseType = "UnsupportedDB"
        };

        // Act
        var result = _dataGenerator.ValidateSettings(settings);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Messages.Any(m => m.Message.Contains("is not supported")));
    }

    [TestMethod]
    public void ValidateSettings_LargeRecordCount_ShouldReturnWarning()
    {
        // Arrange
        var settings = new GenerationSettings
        {
            RecordCount = 15000, // Large number
            DatabaseType = "MySQL"
        };

        // Act
        var result = _dataGenerator.ValidateSettings(settings);

        // Assert
        Assert.IsTrue(result.IsValid); // Should still be valid
        Assert.IsTrue(result.Messages.Any(m => m.Severity == ValidationSeverity.Warning));
        Assert.IsTrue(result.Messages.Any(m => m.Message.Contains("may impact performance")));
    }

    [TestMethod]
    public void ValidateSettings_AIEnabledWithoutApiKey_ShouldReturnWarning()
    {
        // Arrange
        var settings = new GenerationSettings
        {
            RecordCount = 5,
            DatabaseType = "MySQL",
            UseAI = true,
            OpenAiApiKey = null // No API key
        };

        // Act
        var result = _dataGenerator.ValidateSettings(settings);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.Messages.Any(m => m.Severity == ValidationSeverity.Warning));
        Assert.IsTrue(result.Messages.Any(m => m.Message.Contains("no API key is provided")));
    }

    [TestMethod]
    public void Constructor_WithNullDependencies_ShouldThrowException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new DataGenerator(null, _mockTemplateParser.Object, _mockDbExecutor.Object));
        Assert.ThrowsException<ArgumentNullException>(() => new DataGenerator(_mockLogger.Object, null, _mockDbExecutor.Object));
        Assert.ThrowsException<ArgumentNullException>(() => new DataGenerator(_mockLogger.Object, _mockTemplateParser.Object, null));
    }

    [TestMethod]
    public void GenerateData_ExceptionDuringGeneration_ShouldReturnFailure()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Template = "INSERT INTO users (name) VALUES ('{RandomName}')"
        };

        var settings = new GenerationSettings
        {
            RecordCount = 5,
            DatabaseType = "MySQL"
        };

        var validationResult = new TemplateValidationResult { IsValid = true };

        _mockTemplateParser.Setup(x => x.ValidateTemplate(template)).Returns(validationResult);
        _mockTemplateParser.Setup(x => x.GenerateMultiple(template, settings, settings.RecordCount))
                          .Throws(new InvalidOperationException("Template generation failed"));

        // Act
        var result = _dataGenerator.GenerateData(template, settings);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Template generation failed", result.ErrorMessage);
        Assert.IsNotNull(result.Exception);
        Assert.IsTrue(result.ExecutionTime > TimeSpan.Zero);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockLogger = null;
        _mockTemplateParser = null;
        _mockDbExecutor = null;
        _dataGenerator = null;
    }
}