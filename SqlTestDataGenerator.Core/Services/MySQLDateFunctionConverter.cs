using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Kết quả conversion
/// </summary>
public class ConversionResult
{
    public string ConvertedQuery { get; set; } = string.Empty;
    public int ConversionCount { get; set; } = 0;
}

/// <summary>
/// Service để convert date functions sang MySQL syntax chuẩn
/// Xử lý edge cases như CASE statements, nested queries, v.v.
/// </summary>
public class MySQLDateFunctionConverter
{
    private readonly ILogger<MySQLDateFunctionConverter> _logger;

    public MySQLDateFunctionConverter(ILogger<MySQLDateFunctionConverter>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MySQLDateFunctionConverter>.Instance;
    }

    /// <summary>
    /// Convert SQL query để đảm bảo sử dụng MySQL date syntax
    /// </summary>
    public string ConvertToMySQLSyntax(string sqlQuery)
    {
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            return sqlQuery ?? string.Empty;
        }

        var convertedQuery = sqlQuery;
        var conversionCount = 0;

        // 1. Convert datetime('now', '+X days/months/years') to MySQL DATE_ADD
        var result1 = ConvertDatetimeToMySQLDateAdd(convertedQuery);
        convertedQuery = result1.ConvertedQuery;
        conversionCount += result1.ConversionCount;

        // 2. Convert other common date function patterns
        var result2 = ConvertOtherDateFunctions(convertedQuery);
        convertedQuery = result2.ConvertedQuery;
        conversionCount += result2.ConversionCount;

        // 3. Handle CASE statement specific patterns
        var result3 = ConvertCaseStatementDateFunctions(convertedQuery);
        convertedQuery = result3.ConvertedQuery;
        conversionCount += result3.ConversionCount;

        if (conversionCount > 0)
        {
            _logger.LogInformation("Converted {Count} date function(s) to MySQL syntax", conversionCount);
        }

        return convertedQuery;
    }

    /// <summary>
    /// Convert datetime('now', '+30 days') to MySQL DATE_ADD(NOW(), INTERVAL 30 DAY)
    /// </summary>
    private ConversionResult ConvertDatetimeToMySQLDateAdd(string query)
    {
        var conversionCount = 0;
        
        // Pattern: datetime('now', '+N unit') hoặc datetime('now', '-N unit')
        var pattern = @"datetime\s*\(\s*'now'\s*,\s*'([+-])(\d+)\s+(day|days|month|months|year|years)'\s*\)";
        
        var converted = Regex.Replace(query, pattern, match =>
        {
            var sign = match.Groups[1].Value;
            var number = match.Groups[2].Value;
            var unit = match.Groups[3].Value.ToUpper();
            
            // Normalize unit (remove S if present)
            if (unit.EndsWith("S"))
                unit = unit.Substring(0, unit.Length - 1);

            var mysqlFunction = sign == "+" ? "DATE_ADD" : "DATE_SUB";
            var result = $"{mysqlFunction}(NOW(), INTERVAL {number} {unit})";
            
            conversionCount++;
            _logger.LogDebug("Converted datetime to MySQL: {Old} -> {New}", match.Value, result);
            
            return result;
        }, RegexOptions.IgnoreCase);

        return new ConversionResult { ConvertedQuery = converted, ConversionCount = conversionCount };
    }

    /// <summary>
    /// Convert other common date function patterns
    /// </summary>
    private ConversionResult ConvertOtherDateFunctions(string query)
    {
        var result = query;
        var conversionCount = 0;

        // Convert strftime to MySQL DATE_FORMAT (simplified)
        var strftimePattern = @"strftime\s*\(\s*'([^']+)'\s*,\s*([^)]+)\s*\)";
        result = Regex.Replace(result, strftimePattern, match =>
        {
            var format = match.Groups[1].Value;
            var dateExpr = match.Groups[2].Value;
            
            // Basic format conversion (can be expanded)
            var mysqlFormat = format.Replace("%Y", "%Y").Replace("%m", "%m").Replace("%d", "%d");
            var converted = $"DATE_FORMAT({dateExpr}, '{mysqlFormat}')";
            
            conversionCount++;
            _logger.LogDebug("Converted strftime to DATE_FORMAT: {Old} -> {New}", match.Value, converted);
            
            return converted;
        }, RegexOptions.IgnoreCase);

        // Convert date('now') to CURDATE()
        var originalResult = result;
        result = Regex.Replace(result, @"\bdate\s*\(\s*'now'\s*\)", "CURDATE()", RegexOptions.IgnoreCase);
        if (result != originalResult)
        {
            conversionCount++;
            _logger.LogDebug("Converted date('now') to CURDATE()");
        }
        
        // Convert substr to SUBSTRING
        originalResult = result;
        result = Regex.Replace(result, @"\bsubstr\s*\(", "SUBSTRING(", RegexOptions.IgnoreCase);
        if (result != originalResult)
        {
            conversionCount++;
            _logger.LogDebug("Converted substr to SUBSTRING");
        }
        
        return new ConversionResult { ConvertedQuery = result, ConversionCount = conversionCount };
    }

    /// <summary>
    /// Handle CASE statement specific date function conversions
    /// </summary>
    private ConversionResult ConvertCaseStatementDateFunctions(string query)
    {
        var conversionCount = 0;
        
        // Pattern để tìm CASE statements
        var casePattern = @"CASE\s+(?:(?:(?!END\b).)*?)\bEND\b";
        
        var converted = Regex.Replace(query, casePattern, caseMatch =>
        {
            var caseContent = caseMatch.Value;
            var originalCaseContent = caseContent;
            
            // Convert date functions inside CASE statement
            var result1 = ConvertDatetimeToMySQLDateAdd(caseContent);
            caseContent = result1.ConvertedQuery;
            conversionCount += result1.ConversionCount;
            
            var result2 = ConvertOtherDateFunctions(caseContent);
            caseContent = result2.ConvertedQuery;
            conversionCount += result2.ConversionCount;
            
            if (caseContent != originalCaseContent)
            {
                _logger.LogDebug("Converted date functions in CASE statement");
            }
            
            return caseContent;
        }, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return new ConversionResult { ConvertedQuery = converted, ConversionCount = conversionCount };
    }

    /// <summary>
    /// Validate that query contains proper MySQL syntax
    /// </summary>
    public bool ValidateMySQLSyntax(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        // Check for common patterns that should not exist in MySQL
        var invalidPatterns = new[]
        {
            @"datetime\s*\(\s*'now'\s*,",  // datetime('now', ...)
            @"\bsubstr\s*\(",              // substr function
            @"\bdate\s*\(\s*'now'\s*\)"    // date('now')
        };

        foreach (var pattern in invalidPatterns)
        {
            if (Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase))
            {
                _logger.LogWarning("Found invalid syntax in query: {Pattern}", pattern);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Get conversion suggestions for problematic SQL
    /// </summary>
    public List<ConversionSuggestion> GetConversionSuggestions(string query)
    {
        var suggestions = new List<ConversionSuggestion>();

        if (string.IsNullOrWhiteSpace(query))
        {
            return suggestions;
        }

        // Check for datetime patterns
        var datetimeMatches = Regex.Matches(query, @"datetime\s*\(\s*'now'\s*,\s*'([^']+)'\s*\)", RegexOptions.IgnoreCase);
        foreach (Match match in datetimeMatches)
        {
            suggestions.Add(new ConversionSuggestion
            {
                OriginalSyntax = match.Value,
                MySQLSyntax = ConvertToMySQLSyntax(match.Value),
                Description = "Convert datetime() to MySQL DATE_ADD/DATE_SUB",
                Severity = "High"
            });
        }

        // Check for substr patterns
        var substrMatches = Regex.Matches(query, @"\bsubstr\s*\([^)]+\)", RegexOptions.IgnoreCase);
        foreach (Match match in substrMatches)
        {
            var converted = match.Value.Replace("substr", "SUBSTRING", StringComparison.OrdinalIgnoreCase);
            suggestions.Add(new ConversionSuggestion
            {
                OriginalSyntax = match.Value,
                MySQLSyntax = converted,
                Description = "Convert substr() to MySQL SUBSTRING()",
                Severity = "Medium"
            });
        }

        return suggestions;
    }
}

/// <summary>
/// Suggestion for SQL conversion
/// </summary>
public class ConversionSuggestion
{
    public string OriginalSyntax { get; set; } = string.Empty;
    public string MySQLSyntax { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
} 