using System.Globalization;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Centralized value parsing utility to eliminate duplicate parsing logic
/// across multiple services. Replaces scattered TryParse patterns.
/// </summary>
public static class ValueParser
{
    /// <summary>
    /// Parse string value to most appropriate .NET type
    /// Consolidates parsing logic from ConstraintValidator, EngineService, GeminiAIDataGenerationService
    /// </summary>
    public static object ParseToAppropriateType(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            return DBNull.Value;
        
        // Try parsing in order of specificity
        if (TryParseBoolean(value, out var boolValue))
            return boolValue;
            
        if (TryParseInteger(value, out var intValue))
            return intValue;
            
        if (TryParseDecimal(value, out var decimalValue))
            return decimalValue;
            
        if (TryParseDateTime(value, out var dateValue))
            return dateValue;
        
        // Return as string if no specific type matches
        return value;
    }
    
    /// <summary>
    /// Parse value to specific target type
    /// </summary>
    public static T ParseToType<T>(string value, T defaultValue = default)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;
            
        try
        {
            var targetType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            
            if (underlyingType == typeof(string))
                return (T)(object)value;
                
            if (underlyingType == typeof(int))
                return TryParseInteger(value, out var intVal) ? (T)(object)intVal : defaultValue;
                
            if (underlyingType == typeof(decimal))
                return TryParseDecimal(value, out var decVal) ? (T)(object)decVal : defaultValue;
                
            if (underlyingType == typeof(double))
                return TryParseDouble(value, out var doubleVal) ? (T)(object)doubleVal : defaultValue;
                
            if (underlyingType == typeof(bool))
                return TryParseBoolean(value, out var boolVal) ? (T)(object)boolVal : defaultValue;
                
            if (underlyingType == typeof(DateTime))
                return TryParseDateTime(value, out var dateVal) ? (T)(object)dateVal : defaultValue;
                
            // Use Convert.ChangeType as fallback
            return (T)Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        }
        catch
        {
            return defaultValue;
        }
    }
    
    #region Specific Type Parsing
    
    /// <summary>
    /// Enhanced boolean parsing supporting multiple formats
    /// </summary>
    public static bool TryParseBoolean(string value, out bool result)
    {
        result = false;
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        var trimmed = value.Trim().ToLowerInvariant();
        
        // Standard boolean values
        if (bool.TryParse(trimmed, out result))
            return true;
            
        // Numeric boolean (1/0)
        if (int.TryParse(trimmed, out var intVal))
        {
            result = intVal != 0;
            return true;
        }
        
        // Text representations
        switch (trimmed)
        {
            case "yes" or "y" or "on" or "true" or "1":
                result = true;
                return true;
            case "no" or "n" or "off" or "false" or "0":
                result = false;
                return true;
            default:
                return false;
        }
    }
    
    /// <summary>
    /// Enhanced integer parsing with validation
    /// </summary>
    public static bool TryParseInteger(string value, out int result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        // Remove common formatting
        var cleaned = value.Trim().Replace(",", "").Replace("_", "");
        
        return int.TryParse(cleaned, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
    
    /// <summary>
    /// Enhanced decimal parsing with validation
    /// </summary>
    public static bool TryParseDecimal(string value, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        // Remove common formatting
        var cleaned = value.Trim().Replace(",", "").Replace("_", "");
        
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }
    
    /// <summary>
    /// Enhanced double parsing with validation
    /// </summary>
    public static bool TryParseDouble(string value, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        // Remove common formatting
        var cleaned = value.Trim().Replace(",", "").Replace("_", "");
        
        return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
    
    /// <summary>
    /// Enhanced DateTime parsing supporting multiple formats
    /// </summary>
    public static bool TryParseDateTime(string value, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        var trimmed = value.Trim().Trim('\'', '"'); // Remove quotes
        
        // Try standard parsing first
        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            return true;
            
        // Try specific formats
        var formats = new[]
        {
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ"
        };
        
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(trimmed, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return true;
        }
        
        return false;
    }
    
    #endregion
    
    #region Database-Specific Parsing
    
    /// <summary>
    /// Parse value specifically for Oracle database with NULL handling
    /// </summary>
    public static object ParseForOracle(string value, string dataType)
    {
        if (string.IsNullOrEmpty(value) || value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            return DBNull.Value;
            
        var normalizedType = dataType.ToUpper();
        
        return normalizedType switch
        {
            "NUMBER" => TryParseDecimal(value, out var num) ? num : DBNull.Value,
            "DATE" or "TIMESTAMP" => TryParseDateTime(value, out var date) ? date : DBNull.Value,
            "VARCHAR2" or "CHAR" or "CLOB" => value,
            _ => value
        };
    }
    
    /// <summary>
    /// Parse value specifically for MySQL database
    /// </summary>
    public static object ParseForMySQL(string value, string dataType)
    {
        if (string.IsNullOrEmpty(value) || value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            return DBNull.Value;
            
        var normalizedType = dataType.ToUpper();
        
        return normalizedType switch
        {
            "INT" or "INTEGER" or "BIGINT" or "SMALLINT" or "TINYINT" => 
                TryParseInteger(value, out var intVal) ? intVal : DBNull.Value,
            "DECIMAL" or "NUMERIC" or "FLOAT" or "DOUBLE" => 
                TryParseDecimal(value, out var decVal) ? decVal : DBNull.Value,
            "DATETIME" or "TIMESTAMP" or "DATE" => 
                TryParseDateTime(value, out var dateVal) ? dateVal : DBNull.Value,
            "TINYINT(1)" => 
                TryParseBoolean(value, out var boolVal) ? (boolVal ? 1 : 0) : DBNull.Value,
            _ => value
        };
    }
    
    #endregion
    
    #region Validation Utilities
    
    /// <summary>
    /// Check if string represents a valid calendar date (no Feb 30th, etc.)
    /// Used by ConstraintValidator for date validation
    /// </summary>
    public static bool IsValidCalendarDate(string dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return false;
            
        if (!TryParseDateTime(dateString, out var parsedDate))
            return false;
            
        // Basic sanity check: reasonable year range
        if (parsedDate.Year < 1900 || parsedDate.Year > DateTime.Now.Year + 10)
            return false;
            
        // Additional validation: ensure the parsed date matches the input
        // This catches cases like "February 30th" which DateTime.Parse might "correct"
        var dateOnlyString = dateString.Split(' ')[0]; // Remove time component
        
        if (dateOnlyString.Contains("-"))
        {
            var parts = dateOnlyString.Split('-');
            if (parts.Length >= 3 && 
                int.TryParse(parts[0], out int year) && 
                int.TryParse(parts[1], out int month) && 
                int.TryParse(parts[2], out int day))
            {
                // Validate that the parsed date components match input
                return parsedDate.Year == year && parsedDate.Month == month && parsedDate.Day == day;
            }
        }
        
        return true; // For other formats, trust DateTime parsing
    }
    
    /// <summary>
    /// Parse IN clause values (e.g., "('value1', 'value2', 'value3')")
    /// </summary>
    public static List<string> ParseInClauseValues(string inClause)
    {
        if (string.IsNullOrWhiteSpace(inClause))
            return new List<string>();
            
        try
        {
            // Remove outer parentheses and split by comma
            var cleaned = inClause.Trim().Trim('(', ')');
            return cleaned.Split(',')
                .Select(v => v.Trim().Trim('\'', '"'))
                .Where(v => !string.IsNullOrEmpty(v))
                .ToList();
        }
        catch
        {
            return new List<string> { inClause };
        }
    }
    
    /// <summary>
    /// Generate reasonable value from string hash when parsing fails
    /// Used as fallback in data generation
    /// </summary>
    public static object GenerateFromHash(string input, Type targetType)
    {
        if (string.IsNullOrEmpty(input))
            return GetDefaultValue(targetType);
            
        var hash = Math.Abs(input.GetHashCode());
        
        if (targetType == typeof(int))
            return hash % 10000 + 1;
            
        if (targetType == typeof(decimal))
            return (decimal)(hash % 10000) + 0.50m;
            
        if (targetType == typeof(double))
            return (double)(hash % 10000) + 0.50;
            
        if (targetType == typeof(bool))
            return hash % 2 == 0;
            
        if (targetType == typeof(DateTime))
            return DateTime.Now.AddDays(-(hash % 365));
            
        return input; // Return original for string or unknown types
    }
    
    /// <summary>
    /// Get default value for type
    /// </summary>
    public static object GetDefaultValue(Type type)
    {
        if (type.IsValueType)
            return Activator.CreateInstance(type);
        return null;
    }
    
    #endregion
} 