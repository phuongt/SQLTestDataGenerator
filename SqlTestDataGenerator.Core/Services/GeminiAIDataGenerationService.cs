using System.Text;
using System.Text.Json;
using SqlTestDataGenerator.Core.Models;
using Serilog;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Gemini AI-powered data generation service với constraint validation và regeneration
/// Enhanced với EnhancedGeminiFlashRotationService với daily API limits
/// Generates meaningful data tuân thủ engine-extracted constraints
/// </summary>
public class GeminiAIDataGenerationService : IAIDataGenerationService
{
    private readonly ILogger _logger;
    private readonly ConstraintValidator _constraintValidator;
    private readonly EnhancedGeminiFlashRotationService _flashRotationService;

    // Configuration constants
    private const int MAX_REGENERATION_ATTEMPTS = 3;

    // Public property to access flash rotation service for UI display
    public EnhancedGeminiFlashRotationService FlashRotationService => _flashRotationService;

    public GeminiAIDataGenerationService(string apiKey)
    {
        _logger = Log.Logger.ForContext<GeminiAIDataGenerationService>();
        _constraintValidator = new ConstraintValidator();
        _flashRotationService = new EnhancedGeminiFlashRotationService(apiKey);
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.Warning("Gemini API key is not provided. AI generation will be disabled.");
        }
        else
        {
            _logger.Information("🤖 AI Service initialized with EnhancedGeminiFlashRotationService");
            var stats = _flashRotationService.GetModelStatistics();
            _logger.Information("🔄 Using {TotalModels} Flash models with {HealthyModels} healthy models", 
                stats["TotalModels"], stats["HealthyModels"]);
            
            var apiStats = _flashRotationService.GetAPIUsageStatistics();
            _logger.Information("📊 Daily API usage: {UsedCalls}/{LimitCalls} calls", 
                apiStats["DailyCallsUsed"], apiStats["DailyCallLimit"]);
        }
    }



    /// <summary>
    /// Generate column value với constraint validation và regeneration
    /// </summary>
    public async Task<object> GenerateColumnValueAsync(GenerationContext context, int recordIndex)
    {
        try
        {
            // Check availability through EnhancedGeminiFlashRotationService
            if (!await IsAvailableAsync())
            {
                _logger.Information("AI unavailable, falling back to constraint-based generation");
                return GenerateFallbackValue(context, recordIndex);
            }

            // Attempt generation with validation và regeneration
            for (int attempt = 1; attempt <= MAX_REGENERATION_ATTEMPTS; attempt++)
            {
                var prompt = BuildEnhancedGenerationPrompt(context, recordIndex, attempt);
                var response = await CallGeminiAPIAsync(prompt);
                var generatedValue = ParseGeminiResponse(response, context);
                
                // Validate generated value against constraints
                if (ValidateGeneratedValueWithContext(generatedValue, context))
                {
                    _logger.Information("AI generated valid value for {ColumnName} on attempt {Attempt}: {Value}", 
                        context.Column.Name, attempt, generatedValue);
                    return generatedValue;
                }
                else
                {
                    _logger.Warning("AI generated invalid value for {ColumnName} on attempt {Attempt}: {Value}", 
                        context.Column.Name, attempt, generatedValue);
                    
                    if (attempt == MAX_REGENERATION_ATTEMPTS)
                    {
                        _logger.Warning("Max regeneration attempts reached for {ColumnName}, using fallback", 
                            context.Column.Name);
                        return GenerateConstraintAwareFallback(context, recordIndex);
                    }
                }
                
                // Add delay between attempts
                await Task.Delay(1000);
            }

            return GenerateConstraintAwareFallback(context, recordIndex);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "AI generation failed for {ColumnName}, using fallback", context.Column.Name);
            return GenerateConstraintAwareFallback(context, recordIndex);
        }
    }

    /// <summary>
    /// Generate multiple records with comprehensive constraint validation
    /// </summary>
    public async Task<List<Dictionary<string, object>>> GenerateValidatedRecordsAsync(
        string tableName,
        TableSchema tableSchema,
        List<WhereCondition> whereConditions,
        int recordCount)
    {
        var validRecords = new List<Dictionary<string, object>>();
        var maxAttempts = recordCount * 2; // Allow double attempts for complex constraints
        var attempts = 0;

        while (validRecords.Count < recordCount && attempts < maxAttempts)
        {
            attempts++;
            
            var record = new Dictionary<string, object>();
            
            // Generate values for each column
            foreach (var column in tableSchema.Columns.Where(c => !c.IsGenerated))
            {
                var context = BuildGenerationContextForColumn(tableName, column, whereConditions, tableSchema);
                var value = await GenerateColumnValueAsync(context, validRecords.Count + 1);
                record[column.ColumnName] = value;
            }
            
            // Validate entire record against constraints
            var validationResult = _constraintValidator.ValidateConstraints(
                record, tableName, whereConditions, tableSchema);
            
            if (validationResult.IsValid)
            {
                validRecords.Add(record);
                _logger.Information("Generated valid record {RecordIndex} for {TableName}", 
                    validRecords.Count, tableName);
            }
            else
            {
                _logger.Warning("Generated invalid record for {TableName}, violations: {ViolationCount}", 
                    tableName, validationResult.ViolatedConstraints.Count);
                
                // Log constraint violations for debugging
                foreach (var violation in validationResult.ViolatedConstraints.Take(3))
                {
                    _logger.Debug("Constraint violation: {Description}", violation.Description);
                }
            }
        }

        if (validRecords.Count < recordCount)
        {
            _logger.Warning("Could only generate {ActualCount} valid records out of {DesiredCount} for {TableName}", 
                validRecords.Count, recordCount, tableName);
        }

        return validRecords;
    }

    /// <summary>
    /// Build generation context for specific column
    /// </summary>
    private GenerationContext BuildGenerationContextForColumn(
        string tableName,
        ColumnSchema column,
        List<WhereCondition> whereConditions,
        TableSchema tableSchema)
    {
        var relevantConditions = whereConditions
            .Where(c => c.ColumnName.Equals(column.ColumnName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new GenerationContext
        {
            TableName = tableName,
            Column = new ColumnContext
            {
                Name = column.ColumnName,
                DataType = column.DataType,
                MaxLength = column.MaxLength,
                IsRequired = !column.IsNullable,
                IsUnique = column.IsUnique,
                IsPrimaryKey = column.IsPrimaryKey,
                IsGenerated = column.IsGenerated,
                EnumValues = column.EnumValues,
                DefaultValue = column.DefaultValue
            },
            SqlConditions = relevantConditions.Select(c => new ConditionInfo
            {
                Operator = c.Operator,
                Value = c.Value,
                Pattern = c.Operator.ToUpper() == "LIKE" ? c.Value : "",
                InValues = c.Operator.ToUpper() == "IN" ? ParseInValues(c.Value) : new List<object>()
            }).ToList(),
            BusinessHints = new BusinessContext
            {
                Domain = DetermineDomain(tableName),
                SemanticHints = ExtractSemanticHints(column.ColumnName),
                PurposeContext = "test_data_generation"
            }
        };
    }

    /// <summary>
    /// Enhanced validation với comprehensive constraint checking
    /// </summary>
    private bool ValidateGeneratedValueWithContext(object value, GenerationContext context)
    {
        if (value == null) return !context.Column.IsRequired;

        var stringValue = value.ToString() ?? "";

        // Basic validations
        if (!ValidateGeneratedValue(value, context))
        {
            return false;
        }

        // Enhanced SQL condition validation
        foreach (var condition in context.SqlConditions)
        {
            if (!ValidateSqlCondition(stringValue, condition))
            {
                _logger.Debug("Value '{Value}' failed SQL condition: {Operator} {Pattern}", 
                    stringValue, condition.Operator, condition.Pattern ?? condition.Value);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validate SQL condition constraints
    /// </summary>
    private bool ValidateSqlCondition(string value, ConditionInfo condition)
    {
        switch (condition.Operator.ToUpper())
        {
            case "=":
                return value.Equals(condition.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
            
            case "LIKE":
                return ValidateLikePattern(value, condition.Pattern);
            
            case "IN":
                if (condition.InValues == null || !condition.InValues.Any()) return false;
                var stringInValues = condition.InValues.Select(v => v?.ToString() ?? "").ToList();
                return stringInValues.Contains(value, StringComparer.OrdinalIgnoreCase);
            
            case ">":
            case ">=":
            case "<":
            case "<=":
                return ValidateNumericCondition(value, condition);
            
            default:
                return true; // Unknown condition, assume valid
        }
    }

    /// <summary>
    /// Validate LIKE pattern matching
    /// </summary>
    private bool ValidateLikePattern(string value, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return true;

        // Handle common LIKE patterns
        if (pattern.StartsWith("%") && pattern.EndsWith("%"))
        {
            var searchTerm = pattern.Trim('%');
            return value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
        }
        
        if (pattern.StartsWith("%"))
        {
            var suffix = pattern.TrimStart('%');
            return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }
        
        if (pattern.EndsWith("%"))
        {
            var prefix = pattern.TrimEnd('%');
            return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return value.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Build enhanced prompt với specific constraint requirements
    /// </summary>
    private string BuildEnhancedGenerationPrompt(GenerationContext context, int recordIndex, int attempt)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine($"Generate realistic data for database testing (Attempt {attempt}/{MAX_REGENERATION_ATTEMPTS}):");
        prompt.AppendLine($"- Table: {context.TableName}");
        prompt.AppendLine($"- Column: {context.Column.Name}");
        prompt.AppendLine($"- Data Type: {context.Column.DataType}");
        
        if (context.Column.MaxLength.HasValue)
        {
            prompt.AppendLine($"- Maximum Length: {context.Column.MaxLength} characters");
        }
        
        if (context.Column.IsRequired)
        {
            prompt.AppendLine("- Value is REQUIRED (cannot be null/empty)");
        }
        
        if (context.Column.IsUnique)
        {
            prompt.AppendLine($"- Value must be UNIQUE (record #{recordIndex})");
        }

        // Enhanced constraint instructions
        if (context.SqlConditions.Any())
        {
            prompt.AppendLine("\n🎯 CRITICAL SQL CONSTRAINTS (MUST SATISFY):");
            foreach (var condition in context.SqlConditions)
            {
                switch (condition.Operator.ToUpper())
                {
                    case "=":
                        prompt.AppendLine($"  ✅ Must EXACTLY equal: '{condition.Value}'");
                        break;
                    case "LIKE":
                        var pattern = condition.Pattern;
                        if (pattern.StartsWith("%") && pattern.EndsWith("%"))
                        {
                            var searchTerm = pattern.Trim('%');
                            prompt.AppendLine($"  ✅ Must CONTAIN the text: '{searchTerm}'");
                            prompt.AppendLine($"     Example: 'ABC{searchTerm}XYZ' or '{searchTerm}Company' or 'My{searchTerm}'");
                        }
                        else
                        {
                            prompt.AppendLine($"  ✅ Must match LIKE pattern: '{pattern}'");
                        }
                        break;
                    case "IN":
                        prompt.AppendLine($"  ✅ Must be one of: {string.Join(", ", condition.InValues)}");
                        break;
                }
            }
        }

        // Business context hints
        if (context.BusinessHints.SemanticHints.Any())
        {
            prompt.AppendLine($"\n💡 Format hints: {string.Join(", ", context.BusinessHints.SemanticHints)}");
        }

        // Attempt-specific guidance
        if (attempt > 1)
        {
            prompt.AppendLine($"\n⚠️  Previous attempt failed validation. Be more precise with constraints.");
        }

        prompt.AppendLine("\n📝 IMPORTANT: Return ONLY the generated value, no explanation. Value must satisfy ALL constraints above.");
        
        return prompt.ToString();
    }

    /// <summary>
    /// Generate constraint-aware fallback when AI fails
    /// </summary>
    private object GenerateConstraintAwareFallback(GenerationContext context, int recordIndex)
    {
        _logger.Information("Generating constraint-aware fallback for {ColumnName}", context.Column.Name);

        // Check for specific SQL conditions
        foreach (var condition in context.SqlConditions)
        {
            switch (condition.Operator.ToUpper())
            {
                case "=":
                    return condition.Value ?? $"Value_{recordIndex}";
                
                case "LIKE":
                    return GenerateLikePatternValue(condition.Pattern, recordIndex);
                
                case "IN":
                    if (condition.InValues?.Any() == true)
                    {
                        var random = new Random();
                        return condition.InValues[random.Next(condition.InValues.Count)]?.ToString() ?? $"Value_{recordIndex}";
                    }
                    break;
            }
        }

        // Standard fallback
        return GenerateFallbackValue(context, recordIndex);
    }

    /// <summary>
    /// Generate value that matches LIKE pattern
    /// </summary>
    private string GenerateLikePatternValue(string pattern, int recordIndex)
    {
        if (string.IsNullOrEmpty(pattern)) return $"Value_{recordIndex}";

        if (pattern.StartsWith("%") && pattern.EndsWith("%"))
        {
            var searchTerm = pattern.Trim('%');
            return $"Test{searchTerm}Data_{recordIndex}";
        }
        
        if (pattern.StartsWith("%"))
        {
            var suffix = pattern.TrimStart('%');
            return $"Test_{suffix}";
        }
        
        if (pattern.EndsWith("%"))
        {
            var prefix = pattern.TrimEnd('%');
            return $"{prefix}_Data_{recordIndex}";
        }

        return pattern.Replace("_", "X"); // Replace SQL wildcards
    }

    public async Task<List<object>> GenerateColumnValuesAsync(GenerationContext context, int count)
    {
        var values = new List<object>();
        
        for (int i = 0; i < count; i++)
        {
            var value = await GenerateColumnValueAsync(context, i + 1);
            values.Add(value);
        }
        
        return values;
    }

    public bool ValidateGeneratedValue(object value, GenerationContext context)
    {
        if (value == null) return context.Column.IsRequired == false;

        var stringValue = value.ToString() ?? "";

        // Check LENGTH constraint
        var lengthConstraint = context.Constraints.FirstOrDefault(c => c.Type == "LENGTH");
        if (lengthConstraint != null && lengthConstraint.MaxValue != null)
        {
            if (stringValue.Length > Convert.ToInt32(lengthConstraint.MaxValue))
            {
                return false;
            }
        }

        // Check ENUM constraint
        var enumConstraint = context.Constraints.FirstOrDefault(c => c.Type == "ENUM");
        if (enumConstraint != null && enumConstraint.AllowedValues.Count > 0)
        {
            if (!enumConstraint.AllowedValues.Contains(stringValue))
            {
                return false;
            }
        }

        // Check NOT NULL constraint
        if (context.Column.IsRequired && string.IsNullOrEmpty(stringValue))
        {
            return false;
        }

        return true;
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            // Check availability through EnhancedGeminiFlashRotationService
            return _flashRotationService.CanCallAPINow();
        }
        catch
        {
            return false;
        }
    }

    #region Helper Methods

    /// <summary>
    /// Enhanced CallGeminiAPIAsync với Flash Model Rotation và Smart Retry Logic
    /// </summary>
    private async Task<string> CallGeminiAPIAsync(string prompt)
    {
        // Delegate all API calls to EnhancedGeminiFlashRotationService
        // với comprehensive daily limits và time availability checking
        return await _flashRotationService.CallGeminiAPIAsync(prompt, 100);
    }

    private object ParseGeminiResponse(string response, GenerationContext context)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(response);
            var candidates = jsonDoc.RootElement.GetProperty("candidates");
            
            if (candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                var content = firstCandidate.GetProperty("content");
                var parts = content.GetProperty("parts");
                
                if (parts.GetArrayLength() > 0)
                {
                    var text = parts[0].GetProperty("text").GetString()?.Trim();
                    
                    if (!string.IsNullOrEmpty(text))
                    {
                        return ConvertToColumnType(text, context);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to parse Gemini response: {Response}", response);
        }

        return GenerateFallbackValue(context, 1);
    }

    private object ConvertToColumnType(string value, GenerationContext context)
    {
        var dataType = context.Column.DataType.ToLower();

        try
        {
            return dataType switch
            {
                var dt when dt.Contains("int") => int.Parse(value),
                var dt when dt.Contains("decimal") || dt.Contains("numeric") => decimal.Parse(value),
                var dt when dt.Contains("float") || dt.Contains("double") => double.Parse(value),
                var dt when dt.Contains("bool") || dt.Contains("bit") => bool.Parse(value),
                var dt when dt.Contains("date") || dt.Contains("time") => DateTime.Parse(value),
                _ => value
            };
        }
        catch
        {
            return value; // Return as string if conversion fails
        }
    }

    private object GenerateFallbackValue(GenerationContext context, int recordIndex)
    {
        var dataType = context.Column.DataType.ToLower();

        // ENUM handling
        if (dataType.StartsWith("enum(") && context.Column.EnumValues?.Count > 0)
        {
            var random = new Random();
            return context.Column.EnumValues[random.Next(context.Column.EnumValues.Count)];
        }

        // Basic type handling with constraints
        return dataType switch
        {
            var dt when dt.Contains("int") => recordIndex,
            var dt when dt.Contains("varchar") || dt.Contains("text") || dt.Contains("char") => 
                GenerateConstraintAwareString(context, recordIndex),
            var dt when dt.Contains("bool") || dt.Contains("bit") => recordIndex % 2 == 0,
            var dt when dt.Contains("decimal") || dt.Contains("numeric") => (decimal)(recordIndex * 10.5),
            var dt when dt.Contains("date") || dt.Contains("time") => DateTime.Now.AddDays(-recordIndex),
            _ => $"Value_{recordIndex}"
        };
    }

    private string GenerateConstraintAwareString(GenerationContext context, int recordIndex)
    {
        var maxLength = context.Column.MaxLength ?? 255;
        
        // CRITICAL FIX: Check SQL conditions first
        foreach (var condition in context.SqlConditions)
        {
            switch (condition.Operator.ToUpper())
            {
                case "LIKE":
                    var likeValue = GenerateLikePatternValue(condition.Pattern, recordIndex);
                    if (likeValue.Length <= maxLength)
                    {
                        return likeValue;
                    }
                    // Truncate if needed
                    return likeValue.Substring(0, maxLength);
                    
                case "=":
                    var equalValue = condition.Value?.ToString() ?? $"Value_{recordIndex}";
                    if (equalValue.Length <= maxLength)
                    {
                        return equalValue;
                    }
                    return equalValue.Substring(0, maxLength);
                    
                case "IN":
                    if (condition.InValues?.Any() == true)
                    {
                        var random = new Random();
                        var inValue = condition.InValues[random.Next(condition.InValues.Count)]?.ToString() ?? $"Value_{recordIndex}";
                        if (inValue.Length <= maxLength)
                        {
                            return inValue;
                        }
                        return inValue.Substring(0, maxLength);
                    }
                    break;
            }
        }
        
        // Fallback to basic value
        var baseValue = $"TestData_{recordIndex}";
        if (baseValue.Length > maxLength)
        {
            return baseValue.Substring(0, maxLength);
        }
        
        return baseValue;
    }

    private bool ValidateNumericCondition(string value, ConditionInfo condition)
    {
        try
        {
            if (double.TryParse(value, out var numValue) && 
                double.TryParse(condition.Value?.ToString(), out var expectedValue))
            {
                return condition.Operator.ToUpper() switch
                {
                    ">" => numValue > expectedValue,
                    ">=" => numValue >= expectedValue,
                    "<" => numValue < expectedValue,
                    "<=" => numValue <= expectedValue,
                    _ => true
                };
            }
        }
        catch
        {
            // Ignore parse errors
        }
        return false;
    }

    private string DetermineDomain(string tableName)
    {
        return tableName.ToLower() switch
        {
            var t when t.Contains("user") => "user_management",
            var t when t.Contains("company") || t.Contains("organization") => "company_management",
            var t when t.Contains("role") => "role_management",
            var t when t.Contains("product") => "product_management",
            _ => "general"
        };
    }

    private List<string> ExtractSemanticHints(string columnName)
    {
        var hints = new List<string>();
        var name = columnName.ToLower();

        if (name.Contains("email")) hints.Add("email_format");
        if (name.Contains("phone")) hints.Add("phone_format");
        if (name.Contains("name")) hints.Add("name_format");
        if (name.Contains("code")) hints.Add("code_format");
        if (name.Contains("url")) hints.Add("url_format");
        if (name.Contains("date")) hints.Add("date_format");

        return hints;
    }

    private List<object> ParseInValues(string inClause)
    {
        try
        {
            if (string.IsNullOrEmpty(inClause)) return new List<object>();
            
            // Remove parentheses and split by comma
            var cleaned = inClause.Trim('(', ')');
            return cleaned.Split(',')
                .Select(v => v.Trim().Trim('\'', '"') as object)
                .Where(v => v != null)
                .ToList();
        }
        catch
        {
            return new List<object> { inClause };
        }
    }

    #endregion
} 