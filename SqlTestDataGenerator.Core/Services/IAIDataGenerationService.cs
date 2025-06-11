using SqlTestDataGenerator.Core.Models;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Interface cho AI-powered data generation
/// AI generates meaningful data tuân thủ engine-extracted constraints
/// </summary>
public interface IAIDataGenerationService
{
    /// <summary>
    /// Generate single column value using AI với comprehensive context
    /// </summary>
    /// <param name="context">Generation context từ engine analysis</param>
    /// <param name="recordIndex">Record index for uniqueness</param>
    /// <returns>Generated value meeting all constraints</returns>
    Task<object> GenerateColumnValueAsync(GenerationContext context, int recordIndex);
    
    /// <summary>
    /// Generate multiple values for batch processing
    /// </summary>
    /// <param name="context">Generation context</param>
    /// <param name="count">Number of values to generate</param>
    /// <returns>List of generated values</returns>
    Task<List<object>> GenerateColumnValuesAsync(GenerationContext context, int count);
    
    /// <summary>
    /// Validate generated value meets all constraints
    /// </summary>
    /// <param name="value">Generated value</param>
    /// <param name="context">Original generation context</param>
    /// <returns>True if valid, false otherwise</returns>
    bool ValidateGeneratedValue(object value, GenerationContext context);
    
    /// <summary>
    /// Get AI generation capabilities/status
    /// </summary>
    /// <returns>True if AI service available</returns>
    Task<bool> IsAvailableAsync();
} 