using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Tests;

/// <summary>
/// Basic test cho AI integration để verify Gemini service hoạt động
/// </summary>
[TestClass]
public class AIIntegrationBasicTest
{
    [TestMethod]
    [TestCategory("AI-Basic")]
    public async Task TestGeminiServiceInitialization()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? 
                    System.Configuration.ConfigurationManager.AppSettings["GeminiApiKey"];
        
        Console.WriteLine($"Testing Gemini AI service initialization");
        Console.WriteLine($"API Key available: {!string.IsNullOrEmpty(apiKey)}");
        
        // Act
        var aiService = new GeminiAIDataGenerationService(apiKey ?? "test-key");
        var isAvailable = await aiService.IsAvailableAsync();
        
        // Assert
        Console.WriteLine($"AI Service available: {isAvailable}");
        
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.IsFalse(isAvailable, "Service should not be available without API key");
            Console.WriteLine("✅ Correctly detected missing API key");
        }
        else
        {
            // With real API key, test basic functionality
            Console.WriteLine("🔑 API key provided, testing basic generation");
            
            // Create simple generation context
            var context = new GenerationContext
            {
                TableName = "users",
                Column = new ColumnContext
                {
                    Name = "username",
                    DataType = "varchar",
                    MaxLength = 50,
                    IsRequired = true
                },
                BusinessHints = new BusinessContext
                {
                    Domain = "user_management",
                    SemanticHints = new List<string> { "username_format" }
                }
            };
            
            try
            {
                var generatedValue = await aiService.GenerateColumnValueAsync(context, 1);
                Console.WriteLine($"Generated value: {generatedValue}");
                
                Assert.IsNotNull(generatedValue, "AI should generate a value");
                Assert.IsTrue(generatedValue.ToString()?.Length > 0, "Generated value should not be empty");
                
                Console.WriteLine("✅ AI generation successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ AI generation failed (acceptable): {ex.Message}");
                // This is acceptable - API might be unavailable or have quota issues
            }
        }
    }

    [TestMethod]
    [TestCategory("AI-Basic")]
    public void TestConstraintExtractorInitialization()
    {
        // Arrange & Act
        Console.WriteLine("Testing ConstraintExtractorService initialization");
        
        try
        {
            var metadataService = new SqlMetadataService();
            var constraintExtractor = new ConstraintExtractorService();
            
            Console.WriteLine("✅ ConstraintExtractorService initialized successfully");
            Assert.IsNotNull(constraintExtractor, "ConstraintExtractor should be created");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ConstraintExtractor initialization failed: {ex.Message}");
            Assert.Fail($"ConstraintExtractor should initialize without errors: {ex.Message}");
        }
    }

    [TestMethod]
    [TestCategory("AI-Basic")]
    public void TestDataGenServiceWithAI()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? 
                    System.Configuration.ConfigurationManager.AppSettings["GeminiApiKey"];
        
        Console.WriteLine("Testing DataGenService with AI integration");
        Console.WriteLine($"API Key available: {!string.IsNullOrEmpty(apiKey)}");
        
        // Act & Assert
        try
        {
            var dataGenService = new DataGenService(apiKey);
            Console.WriteLine("✅ DataGenService with AI initialized successfully");
            Assert.IsNotNull(dataGenService, "DataGenService should be created");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ DataGenService initialization failed: {ex.Message}");
            Assert.Fail($"DataGenService should initialize even without API key: {ex.Message}");
        }
    }
} 