using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Tests;

[TestClass]
public class LoggerServiceTests
{
    private LoggingSettings _testSettings = null!;
    private string _testLogPath = null!;

    [TestInitialize]
    public void Setup()
    {
        _testLogPath = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid()}.txt");
        _testSettings = new LoggingSettings
        {
            FilePath = _testLogPath,
            LogLevel = "Information",
            EnableUILogging = true,
            MaxFileSizeMB = 1
        };
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_testLogPath))
        {
            File.Delete(_testLogPath);
        }
    }

    [TestMethod]
    public void LogInfo_ShouldCreateLogEntry()
    {
        // Arrange
        using var logger = new LoggerService(_testSettings);
        var logCreated = false;
        LogEntry? capturedEntry = null;

        logger.LogEntryCreated += entry =>
        {
            logCreated = true;
            capturedEntry = entry;
        };

        // Act
        logger.LogInfo("Test info message", "TEST_CONTEXT", new { TestParam = "value" });

        // Assert
        Assert.IsTrue(logCreated, "Log entry should be created");
        Assert.IsNotNull(capturedEntry, "Captured entry should not be null");
        Assert.AreEqual(LogLevel.Information, capturedEntry.Level);
        Assert.AreEqual("Test info message", capturedEntry.Message);
        Assert.AreEqual("TEST_CONTEXT", capturedEntry.Context);
        Assert.IsTrue(capturedEntry.Parameters?.Contains("TestParam"));
    }

    [TestMethod]
    public void LogError_WithException_ShouldCaptureExceptionDetails()
    {
        // Arrange
        using var logger = new LoggerService(_testSettings);
        Exception exception;
        
        // Create exception with stack trace by throwing and catching it
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }
        
        LogEntry? capturedEntry = null;
        var entryReceived = false;

        // Verify settings first
        Assert.IsTrue(_testSettings.EnableUILogging, "UI Logging should be enabled");

        logger.LogEntryCreated += entry => 
        {
            capturedEntry = entry;
            entryReceived = true;
        };

        // Act
        logger.LogError("Test error message", exception, "ERROR_CONTEXT");

        // Wait a short time to ensure the event is processed
        var timeout = DateTime.Now.AddMilliseconds(100);
        while (!entryReceived && DateTime.Now < timeout)
        {
            Thread.Sleep(1);
        }

        // Debug: Check if any log entries exist at all
        var allEntries = logger.GetLogEntries().ToList();
        Assert.IsTrue(allEntries.Count > 0, $"No log entries found. Expected at least 1, got {allEntries.Count}");

        // Debug: Get the first entry if event handler didn't work
        if (capturedEntry == null && allEntries.Count > 0)
        {
            capturedEntry = allEntries.First();
        }

        // Assert
        Assert.IsNotNull(capturedEntry, "Log entry should not be null");
        Assert.AreEqual(LogLevel.Error, capturedEntry.Level);
        Assert.AreEqual("Test error message", capturedEntry.Message);
        Assert.AreEqual("Test exception", capturedEntry.Exception);
        Assert.IsNotNull(capturedEntry.StackTrace, "Stack trace should not be null");
        Assert.IsTrue(capturedEntry.StackTrace.Contains("LogError_WithException_ShouldCaptureExceptionDetails"), 
            "Stack trace should contain the test method name");
    }

    [TestMethod]
    public void LogMethodEntry_ShouldLogWithCorrectContext()
    {
        // Arrange
        using var logger = new LoggerService(_testSettings);
        LogEntry? capturedEntry = null;

        logger.LogEntryCreated += entry => capturedEntry = entry;

        // Act
        logger.LogMethodEntry("TestMethod", new { param1 = "value1", param2 = 42 });

        // Assert
        Assert.IsNotNull(capturedEntry);
        Assert.AreEqual(LogLevel.Information, capturedEntry.Level);
        Assert.AreEqual("Method Entry: TestMethod", capturedEntry.Message);
        Assert.AreEqual("METHOD_ENTRY", capturedEntry.Context);
        Assert.IsTrue(capturedEntry.Parameters?.Contains("param1"));
        Assert.IsTrue(capturedEntry.Parameters?.Contains("value1"));
    }

    [TestMethod]
    public void LogMethodExit_ShouldLogWithReturnValue()
    {
        // Arrange
        using var logger = new LoggerService(_testSettings);
        LogEntry? capturedEntry = null;

        logger.LogEntryCreated += entry => capturedEntry = entry;

        // Act
        logger.LogMethodExit("TestMethod", new { result = "success", count = 5 });

        // Assert
        Assert.IsNotNull(capturedEntry);
        Assert.AreEqual("Method Exit: TestMethod", capturedEntry.Message);
        Assert.AreEqual("METHOD_EXIT", capturedEntry.Context);
        Assert.IsTrue(capturedEntry.Parameters?.Contains("result"));
        Assert.IsTrue(capturedEntry.Parameters?.Contains("success"));
    }

    [TestMethod]
    public void GetLogEntries_ShouldReturnEntriesInDescendingOrder()
    {
        // Arrange
        using var logger = new LoggerService(_testSettings);

        // Act
        logger.LogInfo("First message");
        Thread.Sleep(10); // Ensure different timestamps
        logger.LogWarning("Second message");
        Thread.Sleep(10);
        logger.LogError("Third message");

        var entries = logger.GetLogEntries().ToList();

        // Assert
        Assert.AreEqual(3, entries.Count);
        Assert.AreEqual("Third message", entries[0].Message); // Most recent first
        Assert.AreEqual("Second message", entries[1].Message);
        Assert.AreEqual("First message", entries[2].Message);
    }

    [TestMethod]
    public void ClearLogs_ShouldRemoveAllEntries()
    {
        // Arrange
        using var logger = new LoggerService(_testSettings);
        logger.LogInfo("Test message 1");
        logger.LogInfo("Test message 2");

        // Act
        logger.ClearLogs();

        // Assert
        var entries = logger.GetLogEntries();
        Assert.AreEqual(0, entries.Count());
    }

    [TestMethod]
    public void DisposedLogger_ShouldNotLog()
    {
        // Arrange
        var logger = new LoggerService(_testSettings);
        var logCreated = false;
        logger.LogEntryCreated += _ => logCreated = true;

        // Act
        logger.Dispose();
        logger.LogInfo("This should not be logged");

        // Assert
        Assert.IsFalse(logCreated, "Disposed logger should not create log entries");
    }

    [TestMethod]
    public void LogWithNullParameters_ShouldNotThrow()
    {
        // Arrange
        using var logger = new LoggerService(_testSettings);
        LogEntry? capturedEntry = null;
        logger.LogEntryCreated += entry => capturedEntry = entry;

        // Act & Assert
        var noExceptionThrown = true;
        try
        {
            logger.LogInfo("Test message", null, null);
        }
        catch
        {
            noExceptionThrown = false;
        }

        Assert.IsTrue(noExceptionThrown, "LogInfo should not throw with null parameters");
        Assert.IsNotNull(capturedEntry);
        Assert.AreEqual("Test message", capturedEntry.Message);
    }

    [TestMethod]
    public void LogLevel_Warning_ShouldCreateWarningEntry()
    {
        // Arrange
        using var logger = new LoggerService(_testSettings);
        LogEntry? capturedEntry = null;
        logger.LogEntryCreated += entry => capturedEntry = entry;

        // Act
        logger.LogWarning("Warning message", "WARN_CONTEXT");

        // Assert
        Assert.IsNotNull(capturedEntry);
        Assert.AreEqual(LogLevel.Warning, capturedEntry.Level);
        Assert.AreEqual("Warning message", capturedEntry.Message);
        Assert.AreEqual("WARN_CONTEXT", capturedEntry.Context);
    }
} 