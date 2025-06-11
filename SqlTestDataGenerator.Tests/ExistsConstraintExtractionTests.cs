using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using FluentAssertions;

namespace SqlTestDataGenerator.Tests;

[TestClass]
public class ExistsConstraintExtractionTests
{
    private ComprehensiveConstraintExtractor _extractor;

    [TestInitialize]
    public void Setup()
    {
        _extractor = new ComprehensiveConstraintExtractor();
    }

    [TestMethod]
    public void ExtractExistsConstraints_SimpleExists_ShouldExtractCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE EXISTS (SELECT 1 FROM orders WHERE orders.user_id = users.id)";

        // Act
        var result = _extractor.ExtractAllConstraints(query);

        // Assert
        result.ExistsConstraints.Should().HaveCount(1);
        result.ExistsConstraints[0].IsExists.Should().BeTrue();
        result.ExistsConstraints[0].ExistsType.Should().Be("EXISTS");
        result.ExistsConstraints[0].SubQuery.Should().Be("SELECT 1 FROM orders WHERE orders.user_id = users.id");
    }

    [TestMethod]
    public void ExtractExistsConstraints_SimpleNotExists_ShouldExtractCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE NOT EXISTS (SELECT 1 FROM orders WHERE orders.user_id = users.id)";

        // Act
        var result = _extractor.ExtractAllConstraints(query);

        // Assert
        result.ExistsConstraints.Should().HaveCount(1);
        result.ExistsConstraints[0].IsExists.Should().BeFalse();
        result.ExistsConstraints[0].ExistsType.Should().Be("NOT_EXISTS");
        result.ExistsConstraints[0].SubQuery.Should().Be("SELECT 1 FROM orders WHERE orders.user_id = users.id");
    }

    [TestMethod]
    public void ExtractExistsConstraints_BothExistsAndNotExists_ShouldExtractBothCorrectly()
    {
        // Arrange
        var query = @"SELECT * FROM users 
                     WHERE EXISTS (SELECT 1 FROM orders WHERE orders.user_id = users.id)
                     AND NOT EXISTS (SELECT 1 FROM inactive_users WHERE inactive_users.user_id = users.id)";

        // Act
        var result = _extractor.ExtractAllConstraints(query);

        // Assert
        result.ExistsConstraints.Should().HaveCount(2);
        
        // Verify NOT EXISTS constraint
        var notExistsConstraint = result.ExistsConstraints.FirstOrDefault(c => c.ExistsType == "NOT_EXISTS");
        notExistsConstraint.Should().NotBeNull();
        notExistsConstraint.IsExists.Should().BeFalse();
        notExistsConstraint.SubQuery.Should().Be("SELECT 1 FROM inactive_users WHERE inactive_users.user_id = users.id");

        // Verify EXISTS constraint
        var existsConstraint = result.ExistsConstraints.FirstOrDefault(c => c.ExistsType == "EXISTS");
        existsConstraint.Should().NotBeNull();
        existsConstraint.IsExists.Should().BeTrue();
        existsConstraint.SubQuery.Should().Be("SELECT 1 FROM orders WHERE orders.user_id = users.id");
    }

    [TestMethod]
    public void ExtractExistsConstraints_MultipleWhitespaces_ShouldExtractCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE NOT    EXISTS (SELECT 1 FROM orders WHERE orders.user_id = users.id)";

        // Act
        var result = _extractor.ExtractAllConstraints(query);

        // Assert
        result.ExistsConstraints.Should().HaveCount(1);
        result.ExistsConstraints[0].IsExists.Should().BeFalse();
        result.ExistsConstraints[0].ExistsType.Should().Be("NOT_EXISTS");
        result.ExistsConstraints[0].SubQuery.Should().Be("SELECT 1 FROM orders WHERE orders.user_id = users.id");
    }

    [TestMethod]
    public void ExtractExistsConstraints_NestedParentheses_ShouldHandleCorrectly()
    {
        // Arrange
        var query = @"SELECT * FROM users 
                     WHERE EXISTS (
                         SELECT 1 FROM orders o 
                         WHERE o.user_id = users.id 
                         AND EXISTS (SELECT 1 FROM order_items oi WHERE oi.order_id = o.id)
                     )";

        // Act
        var result = _extractor.ExtractAllConstraints(query);

        // Assert
        result.ExistsConstraints.Should().HaveCount(1);
        result.ExistsConstraints[0].IsExists.Should().BeTrue();
        result.ExistsConstraints[0].ExistsType.Should().Be("EXISTS");
        result.ExistsConstraints[0].SubQuery.Should().Contain("SELECT 1 FROM orders o");
        result.ExistsConstraints[0].SubQuery.Should().Contain("AND EXISTS (SELECT 1 FROM order_items oi WHERE oi.order_id = o.id)");
    }

    [TestMethod]
    public void ExtractExistsConstraints_MultipleExistsAndNotExists_ShouldExtractAllCorrectly()
    {
        // Arrange
        var query = @"SELECT * FROM users u
                     WHERE EXISTS (SELECT 1 FROM orders o WHERE o.user_id = u.id)
                     AND NOT EXISTS (SELECT 1 FROM banned_users b WHERE b.user_id = u.id)
                     AND EXISTS (SELECT 1 FROM user_profiles p WHERE p.user_id = u.id)
                     AND NOT EXISTS (SELECT 1 FROM deleted_users d WHERE d.user_id = u.id)";

        // Act
        var result = _extractor.ExtractAllConstraints(query);

        // Assert
        result.ExistsConstraints.Should().HaveCount(4);
        
        var existsConstraints = result.ExistsConstraints.Where(c => c.ExistsType == "EXISTS").ToList();
        var notExistsConstraints = result.ExistsConstraints.Where(c => c.ExistsType == "NOT_EXISTS").ToList();
        
        existsConstraints.Should().HaveCount(2);
        notExistsConstraints.Should().HaveCount(2);
        
        // Verify all have correct IsExists values
        existsConstraints.All(c => c.IsExists).Should().BeTrue();
        notExistsConstraints.All(c => !c.IsExists).Should().BeTrue();
    }

    [TestMethod]
    public void ExtractExistsConstraints_NoExistsStatements_ShouldReturnEmpty()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE name = 'John' AND age > 25";

        // Act
        var result = _extractor.ExtractAllConstraints(query);

        // Assert
        result.ExistsConstraints.Should().BeEmpty();
    }

    [TestMethod]
    public void ExtractExistsConstraints_CaseInsensitive_ShouldExtractCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE exists (SELECT 1 FROM orders WHERE orders.user_id = users.id) AND not exists (SELECT 1 FROM inactive WHERE inactive.user_id = users.id)";

        // Act
        var result = _extractor.ExtractAllConstraints(query);

        // Assert
        result.ExistsConstraints.Should().HaveCount(2);
        
        var existsConstraint = result.ExistsConstraints.FirstOrDefault(c => c.ExistsType == "EXISTS");
        var notExistsConstraint = result.ExistsConstraints.FirstOrDefault(c => c.ExistsType == "NOT_EXISTS");
        
        existsConstraint.Should().NotBeNull();
        existsConstraint.IsExists.Should().BeTrue();
        
        notExistsConstraint.Should().NotBeNull();
        notExistsConstraint.IsExists.Should().BeFalse();
    }
} 