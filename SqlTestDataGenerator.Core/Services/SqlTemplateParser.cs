using SqlTestDataGenerator.Core.Models;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Service for parsing SQL templates and replacing placeholders with generated data
/// </summary>
public interface ISqlTemplateParser
{
    /// <summary>
    /// Parse SQL template and extract placeholders
    /// </summary>
    TemplateValidationResult ParseTemplate(SqlTemplate template);

    /// <summary>
    /// Replace placeholders in template with generated values
    /// </summary>
    string ReplacePlaceholders(SqlTemplate template, GenerationSettings settings);

    /// <summary>
    /// Generate multiple SQL statements from template
    /// </summary>
    List<string> GenerateMultiple(SqlTemplate template, GenerationSettings settings, int count);

    /// <summary>
    /// Validate template syntax and placeholders
    /// </summary>
    TemplateValidationResult ValidateTemplate(SqlTemplate template);

    /// <summary>
    /// Get available placeholder types
    /// </summary>
    List<PlaceholderType> GetAvailablePlaceholders();
}

/// <summary>
/// Implementation of SQL template parser
/// </summary>
public class SqlTemplateParser : ISqlTemplateParser
{
    private readonly ILoggerService _logger;
    private readonly Random _random;
    private readonly Dictionary<string, Func<Dictionary<string, object>, object>> _placeholderGenerators;

    /// <summary>
    /// Regex pattern for finding placeholders in templates
    /// </summary>
    private static readonly Regex PlaceholderRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public SqlTemplateParser(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogMethodEntry(nameof(SqlTemplateParser));

        _random = new Random();
        _placeholderGenerators = InitializePlaceholderGenerators();

        _logger.LogMethodExit(nameof(SqlTemplateParser));
    }

    public TemplateValidationResult ParseTemplate(SqlTemplate template)
    {
        _logger.LogMethodEntry(nameof(ParseTemplate), new { templateId = template.Id, templateName = template.Name });

        try
        {
            var result = new TemplateValidationResult();
            
            if (string.IsNullOrEmpty(template.Template))
            {
                result.IsValid = false;
                result.Messages.Add(new ValidationMessage
                {
                    Severity = ValidationSeverity.Error,
                    Message = "Template cannot be empty",
                    Field = "Template"
                });
                return result;
            }

            // Find all placeholders in template
            var matches = PlaceholderRegex.Matches(template.Template);
            var placeholders = new HashSet<string>();

            foreach (Match match in matches)
            {
                var placeholderName = match.Groups[1].Value;
                placeholders.Add(placeholderName);
                
                // Validate placeholder type
                if (!_placeholderGenerators.ContainsKey(placeholderName))
                {
                    result.Messages.Add(new ValidationMessage
                    {
                        Severity = ValidationSeverity.Warning,
                        Message = $"Unknown placeholder type: {placeholderName}",
                        Field = "Template"
                    });
                }
            }

            result.ParsedPlaceholders = placeholders.ToList();
            result.IsValid = !result.Messages.Any(m => m.Severity == ValidationSeverity.Error);

            _logger.LogInfo($"Parsed template with {placeholders.Count} placeholders", nameof(ParseTemplate));
            _logger.LogMethodExit(nameof(ParseTemplate), result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error parsing template: {ex.Message}", ex, nameof(ParseTemplate));
            throw;
        }
    }

    public string ReplacePlaceholders(SqlTemplate template, GenerationSettings settings)
    {
        _logger.LogMethodEntry(nameof(ReplacePlaceholders), new { templateId = template.Id, settings });

        try
        {
            var result = template.Template;
            var matches = PlaceholderRegex.Matches(template.Template);

            foreach (Match match in matches)
            {
                var placeholderName = match.Groups[1].Value;
                var placeholderValue = GeneratePlaceholderValue(placeholderName, settings);
                
                result = result.Replace(match.Value, placeholderValue.ToString() ?? string.Empty);
            }

            _logger.LogInfo($"Replaced {matches.Count} placeholders in template", nameof(ReplacePlaceholders));
            _logger.LogMethodExit(nameof(ReplacePlaceholders), new { resultLength = result.Length });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error replacing placeholders: {ex.Message}", ex, nameof(ReplacePlaceholders));
            throw;
        }
    }

    public List<string> GenerateMultiple(SqlTemplate template, GenerationSettings settings, int count)
    {
        _logger.LogMethodEntry(nameof(GenerateMultiple), new { templateId = template.Id, count });

        try
        {
            var results = new List<string>();
            
            for (int i = 0; i < count; i++)
            {
                var sql = ReplacePlaceholders(template, settings);
                results.Add(sql);
            }

            _logger.LogInfo($"Generated {count} SQL statements from template", nameof(GenerateMultiple));
            _logger.LogMethodExit(nameof(GenerateMultiple), new { resultCount = results.Count });
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating multiple SQL statements: {ex.Message}", ex, nameof(GenerateMultiple));
            throw;
        }
    }

    public TemplateValidationResult ValidateTemplate(SqlTemplate template)
    {
        _logger.LogMethodEntry(nameof(ValidateTemplate), new { templateId = template.Id });

        try
        {
            var result = ParseTemplate(template);
            
            // Additional validation rules
            ValidateBasicSqlSyntax(template, result);
            ValidateRequiredPlaceholders(template, result);
            ValidateCustomRules(template, result);

            _logger.LogInfo($"Template validation completed. IsValid: {result.IsValid}", nameof(ValidateTemplate));
            _logger.LogMethodExit(nameof(ValidateTemplate), result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error validating template: {ex.Message}", ex, nameof(ValidateTemplate));
            throw;
        }
    }

    public List<PlaceholderType> GetAvailablePlaceholders()
    {
        _logger.LogMethodEntry(nameof(GetAvailablePlaceholders));

        try
        {
            var placeholders = new List<PlaceholderType>
            {
                new() { Name = "RandomString", Description = "Random string value", DataType = "string", Example = "abc123" },
                new() { Name = "RandomInt", Description = "Random integer value", DataType = "int", Example = "42" },
                new() { Name = "RandomDate", Description = "Random date value", DataType = "datetime", Example = "2024-01-15" },
                new() { Name = "GUID", Description = "Unique identifier", DataType = "string", Example = "12345678-1234-1234-1234-123456789012" },
                new() { Name = "RandomEmail", Description = "Random email address", DataType = "string", Example = "user@example.com" },
                new() { Name = "RandomPhone", Description = "Random phone number", DataType = "string", Example = "+1-555-123-4567" },
                new() { Name = "RandomBoolean", Description = "Random boolean value", DataType = "bool", Example = "true" },
                new() { Name = "RandomDecimal", Description = "Random decimal value", DataType = "decimal", Example = "123.45" },
                new() { Name = "RandomName", Description = "Random person name", DataType = "string", Example = "John Doe" },
                new() { Name = "RandomAddress", Description = "Random address", DataType = "string", Example = "123 Main St" },
                new() { Name = "RandomUrl", Description = "Random URL", DataType = "string", Example = "https://example.com" },
                new() { Name = "RandomIPAddress", Description = "Random IP address", DataType = "string", Example = "192.168.1.1" }
            };

            _logger.LogMethodExit(nameof(GetAvailablePlaceholders), new { count = placeholders.Count });
            return placeholders;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting available placeholders: {ex.Message}", ex, nameof(GetAvailablePlaceholders));
            throw;
        }
    }

    private Dictionary<string, Func<Dictionary<string, object>, object>> InitializePlaceholderGenerators()
    {
        return new Dictionary<string, Func<Dictionary<string, object>, object>>(StringComparer.OrdinalIgnoreCase)
        {
            ["RandomString"] = (parameters) => GenerateRandomString(GetParameterValue(parameters, "length", 10)),
            ["RandomInt"] = (parameters) => GenerateRandomInt(
                GetParameterValue(parameters, "min", 1),
                GetParameterValue(parameters, "max", 1000)),
            ["RandomDate"] = (parameters) => GenerateRandomDate(
                GetParameterValue(parameters, "startDate", DateTime.Now.AddYears(-1)),
                GetParameterValue(parameters, "endDate", DateTime.Now)),
            ["GUID"] = (parameters) => Guid.NewGuid().ToString(),
            ["RandomEmail"] = (parameters) => GenerateRandomEmail(),
            ["RandomPhone"] = (parameters) => GenerateRandomPhone(),
            ["RandomBoolean"] = (parameters) => _random.Next(0, 2) == 1,
            ["RandomDecimal"] = (parameters) => GenerateRandomDecimal(
                GetParameterValue(parameters, "min", 0.0),
                GetParameterValue(parameters, "max", 1000.0)),
            ["RandomName"] = (parameters) => GenerateRandomName(),
            ["RandomAddress"] = (parameters) => GenerateRandomAddress(),
            ["RandomUrl"] = (parameters) => GenerateRandomUrl(),
            ["RandomIPAddress"] = (parameters) => GenerateRandomIPAddress()
        };
    }

    private object GeneratePlaceholderValue(string placeholderName, GenerationSettings settings)
    {
        if (_placeholderGenerators.TryGetValue(placeholderName, out var generator))
        {
            var parameters = settings.PlaceholderParameters.GetValueOrDefault(placeholderName, new Dictionary<string, object>());
            return generator(parameters);
        }

        _logger.LogWarning($"Unknown placeholder type: {placeholderName}", nameof(GeneratePlaceholderValue));
        return $"{{Unknown:{placeholderName}}}";
    }

    private T GetParameterValue<T>(Dictionary<string, object> parameters, string key, T defaultValue)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    private int GenerateRandomInt(int min, int max)
    {
        return _random.Next(min, max + 1);
    }

    private DateTime GenerateRandomDate(DateTime startDate, DateTime endDate)
    {
        var range = endDate - startDate;
        var randomDays = _random.Next(0, (int)range.TotalDays + 1);
        return startDate.AddDays(randomDays);
    }

    private string GenerateRandomEmail()
    {
        var names = new[] { "john", "jane", "bob", "alice", "charlie", "diana", "eve", "frank" };
        var domains = new[] { "example.com", "test.org", "sample.net", "demo.co" };
        
        var name = names[_random.Next(names.Length)];
        var domain = domains[_random.Next(domains.Length)];
        var number = _random.Next(1, 1000);
        
        return $"{name}{number}@{domain}";
    }

    private string GenerateRandomPhone()
    {
        return $"+1-{_random.Next(100, 999)}-{_random.Next(100, 999)}-{_random.Next(1000, 9999)}";
    }

    private decimal GenerateRandomDecimal(double min, double max)
    {
        var value = _random.NextDouble() * (max - min) + min;
        return Math.Round((decimal)value, 2);
    }

    private string GenerateRandomName()
    {
        var firstNames = new[] { "John", "Jane", "Bob", "Alice", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
        
        var firstName = firstNames[_random.Next(firstNames.Length)];
        var lastName = lastNames[_random.Next(lastNames.Length)];
        
        return $"{firstName} {lastName}";
    }

    private string GenerateRandomAddress()
    {
        var numbers = _random.Next(1, 9999);
        var streets = new[] { "Main St", "Oak Ave", "Pine Rd", "Elm Dr", "Maple Ln", "Cedar Way", "Birch Pl", "Spruce Ct" };
        var street = streets[_random.Next(streets.Length)];
        
        return $"{numbers} {street}";
    }

    private string GenerateRandomUrl()
    {
        var protocols = new[] { "http://", "https://" };
        var domains = new[] { "example.com", "test.org", "sample.net", "demo.co" };
        var paths = new[] { "", "/home", "/about", "/contact", "/products", "/services" };
        
        var protocol = protocols[_random.Next(protocols.Length)];
        var domain = domains[_random.Next(domains.Length)];
        var path = paths[_random.Next(paths.Length)];
        
        return $"{protocol}{domain}{path}";
    }

    private string GenerateRandomIPAddress()
    {
        return $"{_random.Next(1, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(1, 255)}";
    }

    private void ValidateBasicSqlSyntax(SqlTemplate template, TemplateValidationResult result)
    {
        var sql = template.Template.ToUpper();
        
        if (!sql.Contains("INSERT") && !sql.Contains("SELECT") && !sql.Contains("UPDATE") && !sql.Contains("DELETE"))
        {
            result.Messages.Add(new ValidationMessage
            {
                Severity = ValidationSeverity.Warning,
                Message = "Template does not contain recognized SQL keywords",
                Field = "Template"
            });
        }
    }

    private void ValidateRequiredPlaceholders(SqlTemplate template, TemplateValidationResult result)
    {
        foreach (var placeholder in template.Placeholders.Where(p => p.IsRequired))
        {
            if (!result.ParsedPlaceholders.Contains(placeholder.Name))
            {
                result.Messages.Add(new ValidationMessage
                {
                    Severity = ValidationSeverity.Error,
                    Message = $"Required placeholder '{placeholder.Name}' not found in template",
                    Field = "Template"
                });
                result.IsValid = false;
            }
        }
    }

    private void ValidateCustomRules(SqlTemplate template, TemplateValidationResult result)
    {
        foreach (var rule in template.ValidationRules)
        {
            try
            {
                if (!string.IsNullOrEmpty(rule.Expression))
                {
                    // Simple regex validation for demonstration
                    var regex = new Regex(rule.Expression, RegexOptions.IgnoreCase);
                    if (!regex.IsMatch(template.Template))
                    {
                        result.Messages.Add(new ValidationMessage
                        {
                            Severity = rule.Severity,
                            Message = rule.ErrorMessage,
                            Field = "Template"
                        });
                        
                        if (rule.Severity == ValidationSeverity.Error)
                        {
                            result.IsValid = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Messages.Add(new ValidationMessage
                {
                    Severity = ValidationSeverity.Warning,
                    Message = $"Invalid validation rule '{rule.Name}': {ex.Message}",
                    Field = "ValidationRules"
                });
            }
        }
    }
}

/// <summary>
/// Represents a placeholder type available for use in templates
/// </summary>
public class PlaceholderType
{
    /// <summary>
    /// Placeholder name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this placeholder generates
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Data type of generated value
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Example of generated value
    /// </summary>
    public string Example { get; set; } = string.Empty;

    /// <summary>
    /// Available parameters for configuration
    /// </summary>
    public List<ParameterInfo> Parameters { get; set; } = new();
}

/// <summary>
/// Information about a placeholder parameter
/// </summary>
public class ParameterInfo
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parameter description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Parameter data type
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Default value
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Whether parameter is required
    /// </summary>
    public bool IsRequired { get; set; } = false;
}