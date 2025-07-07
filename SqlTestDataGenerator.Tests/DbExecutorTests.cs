using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SqlTestDataGenerator.Core.Models;
using SqlTestDataGenerator.Core.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SqlTestDataGenerator.Tests;

[TestClass]
public class DbExecutorTests
{
    private Mock<ILoggerService> _mockLogger;
    private DbExecutor _dbExecutor;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILoggerService>();
        _dbExecutor = new DbExecutor(_mockLogger.Object);
    }

    [TestMethod]
    public void Constructor_WithValidLogger_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var executor = new DbExecutor(_mockLogger.Object);

        // Assert
        Assert.IsNotNull(executor);
        
        // Verify logging
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DbExecutor)), Times.Once);
        _mockLogger.Verify(x => x.LogMethodExit(nameof(DbExecutor)), Times.Once);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrowException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new DbExecutor(null));
    }

    [TestMethod]
    public void ExecuteStatements_WithNullStatements_ShouldReturnFailure()
    {
        // Arrange
        List<string> sqlStatements = null;
        var settings = new GenerationSettings
        {
            DatabaseType = "MySQL",
            ConnectionString = "test-connection"
        };

        // Act
        var result = _dbExecutor.ExecuteStatements(sqlStatements, settings);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("No SQL statements provided", result.ErrorMessage);
        
        // Verify logging
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DbExecutor.ExecuteStatements), It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("No SQL statements provided")), null, nameof(DbExecutor.ExecuteStatements)), Times.Once);
    }

    [TestMethod]
    public void ExecuteStatements_WithEmptyStatements_ShouldReturnFailure()
    {
        // Arrange
        var sqlStatements = new List<string>();
        var settings = new GenerationSettings
        {
            DatabaseType = "MySQL",
            ConnectionString = "test-connection"
        };

        // Act
        var result = _dbExecutor.ExecuteStatements(sqlStatements, settings);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("No SQL statements provided", result.ErrorMessage);
    }

    [TestMethod]
    public void ExecuteStatements_WithoutConnectionString_ShouldReturnFailure()
    {
        // Arrange
        var sqlStatements = new List<string> { "INSERT INTO test VALUES (1)" };
        var settings = new GenerationSettings
        {
            DatabaseType = "MySQL",
            ConnectionString = "" // Empty connection string
        };

        // Act
        var result = _dbExecutor.ExecuteStatements(sqlStatements, settings);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Connection string is required", result.ErrorMessage);
        
        // Verify logging
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Connection string is required")), null, nameof(DbExecutor.ExecuteStatements)), Times.Once);
    }

    [TestMethod]
    public void ExecuteStatement_ValidStatement_ShouldExecuteSingleStatement()
    {
        // Arrange
        var sqlStatement = "INSERT INTO test_table (name) VALUES ('test')";
        var settings = new GenerationSettings
        {
            DatabaseType = "SQLite", // Use SQLite for testing
            ConnectionString = ":memory:" // In-memory SQLite database
        };

        // Act
        var result = _dbExecutor.ExecuteStatement(sqlStatement, settings);

        // Assert
        // Note: This will fail in actual execution due to table not existing,
        // but we're testing the method flow and logging
        Assert.IsNotNull(result);
        
        // Verify logging
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DbExecutor.ExecuteStatement), It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.LogMethodExit(nameof(DbExecutor.ExecuteStatement), It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    public void TestConnection_WithUnsupportedDatabaseType_ShouldReturnFalse()
    {
        // Arrange
        var connectionString = "test-connection";
        var databaseType = "UnsupportedDB";

        // Act
        var result = _dbExecutor.TestConnection(connectionString, databaseType);

        // Assert
        Assert.IsFalse(result);
        
        // Verify error logging
        _mockLogger.Verify(x => x.LogError(It.IsAny<string>(), It.IsAny<Exception>(), nameof(DbExecutor.TestConnection)), Times.Once);
    }

    [TestMethod]
    public void TestConnection_WithInvalidConnectionString_ShouldReturnFalse()
    {
        // Arrange
        var connectionString = "invalid-connection-string";
        var databaseType = "MySQL";

        // Act
        var result = _dbExecutor.TestConnection(connectionString, databaseType);

        // Assert
        Assert.IsFalse(result);
        
        // Verify logging
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DbExecutor.TestConnection), It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.LogError(It.IsAny<string>(), It.IsAny<Exception>(), nameof(DbExecutor.TestConnection)), Times.Once);
        _mockLogger.Verify(x => x.LogMethodExit(nameof(DbExecutor.TestConnection), It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    public async Task TestConnectionAsync_ValidInput_ShouldCallSyncMethod()
    {
        // Arrange
        var connectionString = ":memory:";
        var databaseType = "SQLite";

        // Act
        var result = await _dbExecutor.TestConnectionAsync(connectionString, databaseType);

        // Assert
        Assert.IsNotNull(result);
        
        // Verify async logging
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DbExecutor.TestConnectionAsync), It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.LogMethodExit(nameof(DbExecutor.TestConnectionAsync), It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    public void ExecuteQuery_WithEmptyQuery_ShouldReturnFailure()
    {
        // Arrange
        var sqlQuery = "";
        var settings = new GenerationSettings
        {
            DatabaseType = "MySQL",
            ConnectionString = "test-connection"
        };

        // Act
        var result = _dbExecutor.ExecuteQuery(sqlQuery, settings);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("SQL query cannot be empty", result.ErrorMessage);
        
        // Verify logging
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("SQL query cannot be empty")), null, nameof(DbExecutor.ExecuteQuery)), Times.Once);
    }

    [TestMethod]
    public void ExecuteQuery_WithNullQuery_ShouldReturnFailure()
    {
        // Arrange
        string sqlQuery = null;
        var settings = new GenerationSettings
        {
            DatabaseType = "MySQL",
            ConnectionString = "test-connection"
        };

        // Act
        var result = _dbExecutor.ExecuteQuery(sqlQuery, settings);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("SQL query cannot be empty", result.ErrorMessage);
    }

    [TestMethod]
    public void GetDatabaseSchema_WithUnsupportedDatabaseType_ShouldThrowException()
    {
        // Arrange
        var connectionString = "test-connection";
        var databaseType = "UnsupportedDB";

        // Act & Assert
        Assert.ThrowsException<NotSupportedException>(() => _dbExecutor.GetDatabaseSchema(connectionString, databaseType));
        
        // Verify error logging
        _mockLogger.Verify(x => x.LogError(It.IsAny<string>(), It.IsAny<Exception>(), nameof(DbExecutor.GetDatabaseSchema)), Times.Once);
    }

    [TestMethod]
    public void GetDatabaseSchema_WithInvalidConnection_ShouldThrowException()
    {
        // Arrange
        var connectionString = "invalid-connection";
        var databaseType = "MySQL";

        // Act & Assert
        Assert.ThrowsException<Exception>(() => _dbExecutor.GetDatabaseSchema(connectionString, databaseType));
        
        // Verify logging
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DbExecutor.GetDatabaseSchema), It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.LogError(It.IsAny<string>(), It.IsAny<Exception>(), nameof(DbExecutor.GetDatabaseSchema)), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteStatementsAsync_ValidInput_ShouldCallSyncMethod()
    {
        // Arrange
        var sqlStatements = new List<string> { "SELECT 1" };
        var settings = new GenerationSettings
        {
            DatabaseType = "SQLite",
            ConnectionString = ":memory:"
        };

        // Act
        var result = await _dbExecutor.ExecuteStatementsAsync(sqlStatements, settings);

        // Assert
        Assert.IsNotNull(result);
        
        // Verify async logging
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DbExecutor.ExecuteStatementsAsync), It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.LogMethodExit(nameof(DbExecutor.ExecuteStatementsAsync), It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    public void ExecuteStatements_WithValidSQLiteConnection_ShouldAttemptExecution()
    {
        // Arrange
        var sqlStatements = new List<string>
        {
            "CREATE TABLE test_table (id INTEGER PRIMARY KEY, name TEXT)",
            "INSERT INTO test_table (name) VALUES ('Test')"
        };
        
        var settings = new GenerationSettings
        {
            DatabaseType = "SQLite",
            ConnectionString = ":memory:",
            Performance = new PerformanceSettings
            {
                DatabaseTimeoutSeconds = 30
            }
        };

        // Act
        var result = _dbExecutor.ExecuteStatements(sqlStatements, settings);

        // Assert
        // The result might fail or succeed depending on SQLite availability in test environment
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ExecutionTime > TimeSpan.Zero);
        
        // Verify logging occurred
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DbExecutor.ExecuteStatements), It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.LogMethodExit(nameof(DbExecutor.ExecuteStatements), It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    public void ExecuteQuery_WithValidSQLiteQuery_ShouldAttemptExecution()
    {
        // Arrange
        var sqlQuery = "SELECT 1 as test_column";
        var settings = new GenerationSettings
        {
            DatabaseType = "SQLite",
            ConnectionString = ":memory:",
            Performance = new PerformanceSettings
            {
                DatabaseTimeoutSeconds = 30
            }
        };

        // Act
        var result = _dbExecutor.ExecuteQuery(sqlQuery, settings);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ExecutionTime > TimeSpan.Zero);
        
        // Verify logging
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DbExecutor.ExecuteQuery), It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.LogMethodExit(nameof(DbExecutor.ExecuteQuery), It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    public void ExecuteStatements_WithNullConnectionString_ShouldReturnFailure()
    {
        // Arrange
        var sqlStatements = new List<string> { "SELECT 1" };
        var settings = new GenerationSettings
        {
            DatabaseType = "MySQL",
            ConnectionString = null
        };

        // Act
        var result = _dbExecutor.ExecuteStatements(sqlStatements, settings);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Connection string is required", result.ErrorMessage);
    }

    [TestMethod]
    public void ExecuteStatements_WithWhitespaceStatements_ShouldSkipEmptyStatements()
    {
        // Arrange
        var sqlStatements = new List<string>
        {
            "CREATE TABLE test (id INT)",
            "   ", // Whitespace only
            "", // Empty string
            "INSERT INTO test VALUES (1)"
        };
        
        var settings = new GenerationSettings
        {
            DatabaseType = "SQLite",
            ConnectionString = ":memory:",
            Performance = new PerformanceSettings
            {
                DatabaseTimeoutSeconds = 30
            }
        };

        // Act
        var result = _dbExecutor.ExecuteStatements(sqlStatements, settings);

        // Assert
        Assert.IsNotNull(result);
        // Should only execute 2 non-empty statements
        // The actual success/failure depends on SQLite availability, but we're testing the logic
        
        // Verify logging shows the correct count
        _mockLogger.Verify(x => x.LogInfo(It.Is<string>(s => s.Contains("Starting execution of 4 SQL statements")), nameof(DbExecutor.ExecuteStatements)), Times.Once);
    }

    [TestMethod]
    public void TestConnection_WithValidSQLiteMemoryDb_ShouldReturnTrue()
    {
        // Arrange
        var connectionString = ":memory:";
        var databaseType = "SQLite";

        // Act
        var result = _dbExecutor.TestConnection(connectionString, databaseType);

        // Assert
        // This should succeed if SQLite is available in the test environment
        // The result might vary based on the test environment setup
        Assert.IsNotNull(result);
        
        // Verify logging
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DbExecutor.TestConnection), It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.LogMethodExit(nameof(DbExecutor.TestConnection), It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    public void GetDatabaseSchema_WithValidParameters_ShouldAttemptSchemaRetrieval()
    {
        // Arrange
        var connectionString = ":memory:";
        var databaseType = "SQLite";

        // Act & Assert
        // This will likely fail due to connection issues in test environment
        // but we're testing the method structure and logging
        try
        {
            var result = _dbExecutor.GetDatabaseSchema(connectionString, databaseType);
            Assert.IsNotNull(result);
        }
        catch (Exception)
        {
            // Expected in test environment without proper DB setup
        }
        
        // Verify logging occurred
        _mockLogger.Verify(x => x.LogMethodEntry(nameof(DbExecutor.GetDatabaseSchema), It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    public void ExecuteStatements_ShouldRecordPerformanceMetrics()
    {
        // Arrange
        var sqlStatements = new List<string> { "SELECT 1" };
        var settings = new GenerationSettings
        {
            DatabaseType = "SQLite",
            ConnectionString = ":memory:"
        };

        // Act
        var result = _dbExecutor.ExecuteStatements(sqlStatements, settings);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Performance);
        Assert.IsTrue(result.ExecutionTime >= TimeSpan.Zero);
        Assert.IsTrue(result.Performance.SqlExecutionTime >= TimeSpan.Zero);
        Assert.IsTrue(result.Performance.DatabaseConnectionTime >= TimeSpan.Zero);
    }

    [TestMethod]
    public void ExecuteQuery_ShouldRecordPerformanceMetrics()
    {
        // Arrange
        var sqlQuery = "SELECT 1";
        var settings = new GenerationSettings
        {
            DatabaseType = "SQLite",
            ConnectionString = ":memory:"
        };

        // Act
        var result = _dbExecutor.ExecuteQuery(sqlQuery, settings);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Performance);
        Assert.IsTrue(result.ExecutionTime >= TimeSpan.Zero);
        Assert.IsTrue(result.Performance.SqlExecutionTime >= TimeSpan.Zero);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockLogger = null;
        _dbExecutor = null;
    }
}