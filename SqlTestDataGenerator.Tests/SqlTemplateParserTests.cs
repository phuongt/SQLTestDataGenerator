using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlTestDataGenerator.Tests;

[TestClass]
public class SqlTemplateParserTests
{
    private Mock<ILoggerService> _mockLogger;
    private SqlTemplateParser _parser;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILoggerService>();
        _parser = new SqlTemplateParser(_mockLogger.Object);
    }

    [TestMethod]
    public void ParseTemplate_ValidTemplate_ShouldReturnSuccess()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Name = "Test Template",
            Template = "INSERT INTO users (name, email) VALUES ('{RandomName}', '{RandomEmail}')"
        };

        // Act
        var result = _parser.ParseTemplate(template);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(2, result.ParsedPlaceholders.Count);
        Assert.IsTrue(result.ParsedPlaceholders.Contains("RandomName"));
        Assert.IsTrue(result.ParsedPlaceholders.Contains("RandomEmail"));
        
        // Verify logging
        _mockLogger.Verify(
            x => x.LogMethodEntry(nameof(SqlTemplateParser.ParseTemplate), It.IsAny<object>()),
            Times.Once);
        _mockLogger.Verify(
            x => x.LogMethodExit(nameof(SqlTemplateParser.ParseTemplate), It.IsAny<object>()),
            Times.Once);
    }

    [TestMethod]
    public void ParseTemplate_EmptyTemplate_ShouldReturnError()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Name = "Test Template",
            Template = ""
        };

        // Act
        var result = _parser.ParseTemplate(template);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(1, result.Messages.Count);
        Assert.AreEqual(ValidationSeverity.Error, result.Messages[0].Severity);
        Assert.AreEqual("Template cannot be empty", result.Messages[0].Message);
    }

    [TestMethod]
    public void ParseTemplate_UnknownPlaceholder_ShouldReturnWarning()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Name = "Test Template",
            Template = "INSERT INTO users (name) VALUES ('{UnknownPlaceholder}')"
        };

        // Act
        var result = _parser.ParseTemplate(template);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(1, result.Messages.Count);
        Assert.AreEqual(ValidationSeverity.Warning, result.Messages[0].Severity);
        Assert.IsTrue(result.Messages[0].Message.Contains("Unknown placeholder type"));
    }

    [TestMethod]
    public void ReplacePlaceholders_ValidTemplate_ShouldReplaceAllPlaceholders()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Name = "Test Template",
            Template = "INSERT INTO users (name, email, age) VALUES ('{RandomName}', '{RandomEmail}', {RandomInt})"
        };

        var settings = new GenerationSettings
        {
            RecordCount = 1,
            RandomSeed = 12345
        };

        // Act
        var result = _parser.ReplacePlaceholders(template, settings);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Contains("{RandomName}"));
        Assert.IsFalse(result.Contains("{RandomEmail}"));
        Assert.IsFalse(result.Contains("{RandomInt}"));
        Assert.IsTrue(result.StartsWith("INSERT INTO users"));
        
        // Verify logging
        _mockLogger.Verify(
            x => x.LogMethodEntry(nameof(SqlTemplateParser.ReplacePlaceholders), It.IsAny<object>()),
            Times.Once);
        _mockLogger.Verify(
            x => x.LogMethodExit(nameof(SqlTemplateParser.ReplacePlaceholders), It.IsAny<object>()),
            Times.Once);
    }

    [TestMethod]
    public void GenerateMultiple_ValidTemplate_ShouldGenerateMultipleStatements()
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
            RecordCount = 5
        };

        // Act
        var result = _parser.GenerateMultiple(template, settings, 3);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count);
        foreach (var sql in result)
        {
            Assert.IsTrue(sql.StartsWith("INSERT INTO users"));
            Assert.IsFalse(sql.Contains("{RandomName}"));
        }
    }

    [TestMethod]
    public void ValidateTemplate_ValidTemplate_ShouldReturnValid()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Name = "Test Template",
            Template = "INSERT INTO users (name) VALUES ('{RandomName}')"
        };

        // Act
        var result = _parser.ValidateTemplate(template);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Messages.Count(m => m.Severity == ValidationSeverity.Error));
    }

    [TestMethod]
    public void ValidateTemplate_RequiredPlaceholderMissing_ShouldReturnError()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Name = "Test Template",
            Template = "INSERT INTO users (name) VALUES ('static')",
            Placeholders = new List<PlaceholderDefinition>
            {
                new PlaceholderDefinition { Name = "RequiredPlaceholder", IsRequired = true }
            }
        };

        // Act
        var result = _parser.ValidateTemplate(template);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Messages.Any(m => m.Severity == ValidationSeverity.Error));
        Assert.IsTrue(result.Messages.Any(m => m.Message.Contains("Required placeholder")));
    }

    [TestMethod]
    public void ValidateTemplate_NoSqlKeywords_ShouldReturnWarning()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Name = "Test Template",
            Template = "Some random text with {RandomName}"
        };

        // Act
        var result = _parser.ValidateTemplate(template);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.Messages.Any(m => m.Severity == ValidationSeverity.Warning));
        Assert.IsTrue(result.Messages.Any(m => m.Message.Contains("does not contain recognized SQL keywords")));
    }

    [TestMethod]
    public void GetAvailablePlaceholders_ShouldReturnAllPlaceholderTypes()
    {
        // Act
        var result = _parser.GetAvailablePlaceholders();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Count > 0);
        Assert.IsTrue(result.Any(p => p.Name == "RandomString"));
        Assert.IsTrue(result.Any(p => p.Name == "RandomInt"));
        Assert.IsTrue(result.Any(p => p.Name == "RandomDate"));
        Assert.IsTrue(result.Any(p => p.Name == "GUID"));
        Assert.IsTrue(result.Any(p => p.Name == "RandomEmail"));
        
        // Verify each placeholder has required properties
        foreach (var placeholder in result)
        {
            Assert.IsFalse(string.IsNullOrEmpty(placeholder.Name));
            Assert.IsFalse(string.IsNullOrEmpty(placeholder.Description));
            Assert.IsFalse(string.IsNullOrEmpty(placeholder.DataType));
            Assert.IsFalse(string.IsNullOrEmpty(placeholder.Example));
        }
    }

    [TestMethod]
    public void ParseTemplate_WithCustomValidationRule_ShouldValidateRule()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Name = "Test Template",
            Template = "INSERT INTO users (name) VALUES ('{RandomName}')",
            ValidationRules = new List<ValidationRule>
            {
                new ValidationRule
                {
                    Name = "MustContainInsert",
                    Expression = "INSERT",
                    ErrorMessage = "Template must contain INSERT statement",
                    Severity = ValidationSeverity.Error
                }
            }
        };

        // Act
        var result = _parser.ValidateTemplate(template);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Messages.Count(m => m.Severity == ValidationSeverity.Error));
    }

    [TestMethod]
    public void ParseTemplate_WithFailingValidationRule_ShouldReturnError()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Id = "test-template",
            Name = "Test Template",
            Template = "SELECT * FROM users WHERE name = '{RandomName}'",
            ValidationRules = new List<ValidationRule>
            {
                new ValidationRule
                {
                    Name = "MustContainInsert",
                    Expression = "INSERT",
                    ErrorMessage = "Template must contain INSERT statement",
                    Severity = ValidationSeverity.Error
                }
            }
        };

        // Act
        var result = _parser.ValidateTemplate(template);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Messages.Any(m => m.Severity == ValidationSeverity.Error));
        Assert.IsTrue(result.Messages.Any(m => m.Message == "Template must contain INSERT statement"));
    }

    [TestMethod]
    public void ReplacePlaceholders_WithNullTemplate_ShouldThrowException()
    {
        // Arrange
        SqlTemplate template = null;
        var settings = new GenerationSettings();

        // Act & Assert
        Assert.ThrowsException<NullReferenceException>(() => _parser.ReplacePlaceholders(template, settings));
    }

    [TestMethod]
    public void ReplacePlaceholders_WithNullSettings_ShouldThrowException()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Template = "INSERT INTO users (name) VALUES ('{RandomName}')"
        };
        GenerationSettings settings = null;

        // Act & Assert
        Assert.ThrowsException<NullReferenceException>(() => _parser.ReplacePlaceholders(template, settings));
    }

    [TestMethod]
    public void ReplacePlaceholders_WithCustomPlaceholderParameters_ShouldUseParameters()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Template = "INSERT INTO users (name) VALUES ('{RandomString}')"
        };

        var settings = new GenerationSettings
        {
            PlaceholderParameters = new Dictionary<string, Dictionary<string, object>>
            {
                ["RandomString"] = new Dictionary<string, object>
                {
                    ["length"] = 20
                }
            }
        };

        // Act
        var result = _parser.ReplacePlaceholders(template, settings);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Contains("{RandomString}"));
        // The exact value depends on random generation, but we can verify it's replaced
    }

    [TestMethod]
    public void ReplacePlaceholders_WithUnknownPlaceholder_ShouldReplaceWithUnknownMarker()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Template = "INSERT INTO users (name) VALUES ('{UnknownPlaceholder}')"
        };

        var settings = new GenerationSettings();

        // Act
        var result = _parser.ReplacePlaceholders(template, settings);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("{Unknown:UnknownPlaceholder}"));
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.LogWarning(It.Is<string>(s => s.Contains("Unknown placeholder type")), It.IsAny<string>(), It.IsAny<object>()),
            Times.Once);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrowException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new SqlTemplateParser(null));
    }

    [TestMethod]
    public void ValidateTemplate_WithInvalidValidationRule_ShouldReturnWarning()
    {
        // Arrange
        var template = new SqlTemplate
        {
            Template = "INSERT INTO users (name) VALUES ('{RandomName}')",
            ValidationRules = new List<ValidationRule>
            {
                new ValidationRule
                {
                    Name = "InvalidRule",
                    Expression = "[invalid-regex",
                    ErrorMessage = "This should fail",
                    Severity = ValidationSeverity.Error
                }
            }
        };

        // Act
        var result = _parser.ValidateTemplate(template);

        // Assert
        Assert.IsTrue(result.IsValid); // Should still be valid because regex validation failed
        Assert.IsTrue(result.Messages.Any(m => m.Severity == ValidationSeverity.Warning));
        Assert.IsTrue(result.Messages.Any(m => m.Message.Contains("Invalid validation rule")));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockLogger = null;
        _parser = null;
    }
}