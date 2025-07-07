using System.Text.Json.Serialization;

namespace SqlTestDataGenerator.Core.Models;

/// <summary>
/// Represents SQL templates with placeholders for data generation
/// </summary>
public class SqlTemplate
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Human-readable name for the template
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this template does
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The SQL template with placeholders
    /// Placeholders: {RandomString}, {RandomInt}, {RandomDate}, {GUID}, {RandomEmail}, etc.
    /// </summary>
    public string Template { get; set; } = string.Empty;

    /// <summary>
    /// Database type this template is designed for
    /// </summary>
    public string DatabaseType { get; set; } = "MySQL";

    /// <summary>
    /// List of placeholder definitions used in this template
    /// </summary>
    public List<PlaceholderDefinition> Placeholders { get; set; } = new();

    /// <summary>
    /// Template category (e.g., "INSERT", "SELECT", "UPDATE")
    /// </summary>
    public string Category { get; set; } = "INSERT";

    /// <summary>
    /// Tags for categorizing templates
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Whether this template is active/enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sample output for preview purposes
    /// </summary>
    public string? SampleOutput { get; set; }

    /// <summary>
    /// Validation rules for this template
    /// </summary>
    public List<ValidationRule> ValidationRules { get; set; } = new();
}

/// <summary>
/// Defines a placeholder used in SQL templates
/// </summary>
public class PlaceholderDefinition
{
    /// <summary>
    /// Placeholder name (e.g., "RandomString")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Data type of the placeholder value
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this placeholder generates
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Configuration parameters for the placeholder
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Whether this placeholder is required
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Default value if not provided
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Validation pattern for the generated value
    /// </summary>
    public string? ValidationPattern { get; set; }
}

/// <summary>
/// Validation rule for SQL templates
/// </summary>
public class ValidationRule
{
    /// <summary>
    /// Rule name/identifier
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rule description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Validation expression or pattern
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Error message to display if validation fails
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Rule severity level
    /// </summary>
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
}

/// <summary>
/// Validation severity levels
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Information/hint level
    /// </summary>
    Info,

    /// <summary>
    /// Warning level - template can still be used
    /// </summary>
    Warning,

    /// <summary>
    /// Error level - template cannot be used
    /// </summary>
    Error
}

/// <summary>
/// Result of template validation
/// </summary>
public class TemplateValidationResult
{
    /// <summary>
    /// Whether the template is valid
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// List of validation messages
    /// </summary>
    public List<ValidationMessage> Messages { get; set; } = new();

    /// <summary>
    /// Parsed placeholders found in template
    /// </summary>
    public List<string> ParsedPlaceholders { get; set; } = new();
}

/// <summary>
/// Validation message
/// </summary>
public class ValidationMessage
{
    /// <summary>
    /// Message severity
    /// </summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>
    /// Message text
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Field or location where the issue occurred
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Line number in template (if applicable)
    /// </summary>
    public int? LineNumber { get; set; }
}