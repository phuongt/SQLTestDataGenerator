using System.Text.RegularExpressions;
using SqlTestDataGenerator.Core.Models;
using Serilog;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// MySQL-focused SQL query parser with enhanced regex patterns
/// Optimized specifically for MySQL syntax including DATE_ADD, INTERVAL, etc.
/// </summary>
public class SqlQueryParserV3 : ISqlParser
{
    private readonly ILogger _logger;

    public SqlQueryParserV3()
    {
        _logger = Log.ForContext<SqlQueryParserV3>();
    }

    /// <summary>
    /// Parse SQL query to extract data generation requirements using MySQL-specific regex patterns
    /// </summary>
    public SqlDataRequirements ParseQuery(string sqlQuery)
    {
        _logger.Information("SqlQueryParserV3: Parsing SQL query with MySQL-enhanced regex patterns");
        
        var requirements = new SqlDataRequirements
        {
            OriginalQuery = sqlQuery
        };

        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            return requirements;
        }

        try
        {
            var cleanQuery = CleanSqlQuery(sqlQuery);
            
            // Extract table requirements
            ExtractTableRequirements(cleanQuery, requirements);
            
            // Extract WHERE conditions with MySQL-specific handling
            ExtractWhereConditions(cleanQuery, requirements);
            
            // Extract JOIN requirements
            ExtractJoinRequirements(cleanQuery, requirements);

            _logger.Information("MySQL Parser extracted {WhereCount} WHERE conditions and {JoinCount} JOIN requirements", 
                requirements.WhereConditions.Count, requirements.JoinRequirements.Count);

            return requirements;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "MySQL Parser failed, falling back to base regex parser");
            
            // Fallback to original regex parser
            var fallbackParser = new SqlQueryParser();
            return fallbackParser.ParseQuery(sqlQuery);
        }
    }

    private string CleanSqlQuery(string sqlQuery)
    {
        // Remove SQL comments
        var withoutLineComments = Regex.Replace(sqlQuery, @"--[^\r\n]*", "", RegexOptions.Multiline);
        var withoutBlockComments = Regex.Replace(withoutLineComments, @"/\*[\s\S]*?\*/", "", RegexOptions.Multiline);
        
        // Normalize whitespace but preserve structure for MySQL functions
        var normalized = Regex.Replace(withoutBlockComments, @"\s+", " ");
        
        return normalized.Trim();
    }

    private void ExtractTableRequirements(string cleanQuery, SqlDataRequirements requirements)
    {
        // Extract tables from FROM and JOIN clauses - enhanced for MySQL
        var tables = SqlMetadataService.ExtractTablesFromQuery(cleanQuery);
        
        foreach (var table in tables)
        {
            if (!requirements.TableRequirements.ContainsKey(table))
            {
                requirements.TableRequirements[table] = new TableDataRequirement
                {
                    TableName = table
                };
            }
        }

        _logger.Debug("Extracted {TableCount} tables: {Tables}", tables.Count, string.Join(", ", tables));
    }

    private void ExtractWhereConditions(string cleanQuery, SqlDataRequirements requirements)
    {
        // Find WHERE clause - enhanced pattern for MySQL
        var wherePattern = @"\bWHERE\s+(.*?)(?:\s+(?:ORDER\s+BY|GROUP\s+BY|HAVING|LIMIT|$)|$)";
        var whereMatch = Regex.Match(cleanQuery, wherePattern, 
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        _logger.Debug("ExtractWhereConditions: Pattern='{Pattern}', CleanQuery='{CleanQuery}', Success={Success}", 
            wherePattern, cleanQuery, whereMatch.Success);

        if (!whereMatch.Success)
        {
            _logger.Debug("No WHERE clause found");
            return;
        }

        var whereClause = whereMatch.Groups[1].Value;
        _logger.Information("Found WHERE clause: {WhereClause}", whereClause);

        // Parse various MySQL-specific WHERE condition types
        // Note: Order matters - more specific patterns first to avoid duplicates
        ParseBooleanConditions(whereClause, requirements);  // Enhanced for MySQL TRUE/FALSE - FIRST
        ParseMySqlDateConditions(whereClause, requirements);  // MySQL-specific
        ParseLikeConditions(whereClause, requirements);
        ParseNullChecks(whereClause, requirements);
        ParseInConditions(whereClause, requirements);
        ParseComparisonConditions(whereClause, requirements);
        ParseEqualityConditions(whereClause, requirements);  // General equality - LAST to avoid duplicates
    }

    private void ParseEqualityConditions(string whereClause, SqlDataRequirements requirements)
    {
        // Enhanced pattern for MySQL - handles numbers, strings (but NOT TRUE/FALSE which are handled separately)
        var equalityPattern = @"(\w+\.)?(\w+)\s*=\s*(['""]?)([^'"";\s\)\&\|]+)\3";
        var matches = Regex.Matches(whereClause, equalityPattern, RegexOptions.IgnoreCase);

        _logger.Debug("ParseEqualityConditions: Pattern='{Pattern}', WhereClause='{WhereClause}', MatchCount={MatchCount}", 
            equalityPattern, whereClause, matches.Count);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var value = match.Groups[4].Value;

            _logger.Debug("ParseEqualityConditions: Match='{Match}', TableAlias='{TableAlias}', Column='{Column}', Value='{Value}'", 
                match.Value, tableAlias, columnName, value);

            // Skip if it's a JOIN condition (comparing columns)
            if (value.Contains('.') && !IsBooleanOrNumeric(value))
            {
                _logger.Debug("Skipping JOIN condition: {Value}", value);
                continue;
            }

            // Skip if it's a boolean value (TRUE/FALSE) - already handled by ParseBooleanConditions
            if (value.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || 
                value.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Debug("Skipping boolean condition (already parsed): {Value}", value);
                continue;
            }

            // Check if this condition already exists to avoid duplicates
            var existingCondition = requirements.WhereConditions.FirstOrDefault(c => 
                c.TableAlias == tableAlias && 
                c.ColumnName == columnName && 
                c.Value == value);

            if (existingCondition != null)
            {
                _logger.Debug("Skipping duplicate condition: {Column} = '{Value}'", columnName, value);
                continue;
            }

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = "=",
                Value = value,
                OriginalCondition = match.Value
            });

            _logger.Debug("Found equality condition: {Column} = '{Value}'", columnName, value);
        }
    }

    private void ParseBooleanConditions(string whereClause, SqlDataRequirements requirements)
    {
        // MySQL boolean pattern - TRUE/FALSE without quotes
        var boolPattern = @"(\w+\.)?(\w+)\s*=\s*(TRUE|FALSE)\b";
        var matches = Regex.Matches(whereClause, boolPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var value = match.Groups[3].Value;

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = "=",
                Value = value.ToUpper(),
                OriginalCondition = match.Value
            });

            _logger.Debug("Found boolean condition: {Column} = {Value}", columnName, value);
        }
    }

    private void ParseMySqlDateConditions(string whereClause, SqlDataRequirements requirements)
    {
        // MySQL-specific: YEAR(column) = 1989
        var yearPattern = @"YEAR\s*\(\s*(\w+\.)?(\w+)\s*\)\s*=\s*(\d{4})";
        var matches = Regex.Matches(whereClause, yearPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var year = match.Groups[3].Value;

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = "YEAR_EQUALS",
                Value = year,
                OriginalCondition = match.Value
            });

            _logger.Debug("Found MySQL year condition: YEAR({Column}) = {Year}", columnName, year);
        }

        // MySQL-specific: column <= DATE_ADD(NOW(), INTERVAL 30 DAY)
        var dateAddPattern = @"(\w+\.)?(\w+)\s*(<=|>=|<|>)\s*DATE_ADD\s*\(\s*NOW\s*\(\s*\)\s*,\s*INTERVAL\s+(\d+)\s+(DAY|MONTH|YEAR)\s*\)";
        var dateMatches = Regex.Matches(whereClause, dateAddPattern, RegexOptions.IgnoreCase);

        foreach (Match match in dateMatches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var operator_ = match.Groups[3].Value;
            var interval = match.Groups[4].Value;
            var unit = match.Groups[5].Value.ToUpper();

            var constraintValue = $"{interval}_{unit}";
            var operatorName = operator_ == "<=" ? "DATE_WITHIN" : "DATE_COMPARE";

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = operatorName,
                Value = constraintValue,
                OriginalCondition = match.Value
            });

            _logger.Debug("Found MySQL date condition: {Column} {Op} DATE_ADD(NOW(), INTERVAL {Interval} {Unit})", 
                columnName, operator_, interval, unit);
        }

        // MySQL-specific: DATE(column) = '2023-01-01'
        var datePattern = @"DATE\s*\(\s*(\w+\.)?(\w+)\s*\)\s*=\s*['""]([^'""]+)['""]";
        var dateExactMatches = Regex.Matches(whereClause, datePattern, RegexOptions.IgnoreCase);

        foreach (Match match in dateExactMatches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var dateValue = match.Groups[3].Value;

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = "DATE_EQUALS",
                Value = dateValue,
                OriginalCondition = match.Value
            });

            _logger.Debug("Found MySQL date exact condition: DATE({Column}) = '{Date}'", columnName, dateValue);
        }
    }

    private void ParseLikeConditions(string whereClause, SqlDataRequirements requirements)
    {
        // Pattern: column LIKE 'value' or column LIKE '%value%'
        var likePattern = @"(\w+\.)?(\w+)\s+LIKE\s+['""]([^'""]*)['""]";
        var matches = Regex.Matches(whereClause, likePattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var likeValue = match.Groups[3].Value;

            // Extract actual value by removing % wildcards
            var actualValue = likeValue.Replace("%", "").Trim();

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = "LIKE",
                Value = actualValue,
                OriginalCondition = match.Value
            });

            _logger.Debug("Found LIKE condition: {Column} LIKE '{Value}'", columnName, actualValue);
        }
    }

    private void ParseComparisonConditions(string whereClause, SqlDataRequirements requirements)
    {
        // Pattern: column >= value, column > value, column < value, column <= value
        var comparisonPattern = @"(\w+\.)?(\w+)\s*(>=|<=|>|<)\s*(['""]?)([^'"";\s\)\&\|]+)\4";
        var matches = Regex.Matches(whereClause, comparisonPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var operator_ = match.Groups[3].Value;
            var value = match.Groups[5].Value;

            // Skip if it's a column comparison or complex expression
            if (value.Contains('.') || value.Contains('(') || IsReservedWord(value))
            {
                continue;
            }

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = operator_,
                Value = value,
                OriginalCondition = match.Value
            });

            _logger.Debug("Found comparison condition: {Column} {Operator} '{Value}'", columnName, operator_, value);
        }
    }

    private void ParseNullChecks(string whereClause, SqlDataRequirements requirements)
    {
        // Pattern: column IS NULL or column IS NOT NULL
        var nullPattern = @"(\w+\.)?(\w+)\s+IS\s+(NOT\s+)?NULL";
        var matches = Regex.Matches(whereClause, nullPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var isNotNull = !string.IsNullOrEmpty(match.Groups[3].Value);

            requirements.WhereConditions.Add(new WhereCondition
            {
                TableAlias = tableAlias,
                ColumnName = columnName,
                Operator = isNotNull ? "IS_NOT_NULL" : "IS_NULL",
                Value = "",
                OriginalCondition = match.Value
            });

            _logger.Debug("Found null check: {Column} IS {NotNull}NULL", columnName, isNotNull ? "NOT " : "");
        }
    }

    private void ParseInConditions(string whereClause, SqlDataRequirements requirements)
    {
        // Pattern: column IN (value1, value2, value3)
        var inPattern = @"(\w+\.)?(\w+)\s+IN\s*\(\s*([^)]+)\s*\)";
        var matches = Regex.Matches(whereClause, inPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tableAlias = match.Groups[1].Value.TrimEnd('.');
            var columnName = match.Groups[2].Value;
            var valuesList = match.Groups[3].Value;

            // Parse individual values
            var values = valuesList.Split(',')
                .Select(v => v.Trim().Trim('\'', '"'))
                .Where(v => !string.IsNullOrEmpty(v))
                .ToList();

            if (values.Any())
            {
                requirements.WhereConditions.Add(new WhereCondition
                {
                    TableAlias = tableAlias,
                    ColumnName = columnName,
                    Operator = "IN",
                    Value = string.Join(",", values),
                    OriginalCondition = match.Value
                });

                _logger.Debug("Found IN condition: {Column} IN ({Values})", columnName, string.Join(", ", values));
            }
        }
    }

    private void ExtractJoinRequirements(string cleanQuery, SqlDataRequirements requirements)
    {
        // Enhanced JOIN pattern for MySQL
        var joinPattern = @"(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+|OUTER\s+)?JOIN\s+(\w+)\s+(\w+)\s+ON\s+([^()]+?)(?=\s+(?:INNER|LEFT|RIGHT|FULL|OUTER|WHERE|ORDER|GROUP|LIMIT|$)|$)";
        
        _logger.Debug("JOIN Debug: Clean query = '{CleanQuery}'", cleanQuery);
        
        var matches = Regex.Matches(cleanQuery, joinPattern, RegexOptions.IgnoreCase);
        _logger.Debug("JOIN Debug: Found {MatchCount} JOIN matches", matches.Count);

        foreach (Match match in matches)
        {
            var table = match.Groups[1].Value;
            var alias = match.Groups[2].Value;
            var onClause = match.Groups[3].Value;
            
            _logger.Debug("JOIN Debug: Match found - Table: '{Table}', Alias: '{Alias}', OnClause: '{OnClause}'", table, alias, onClause);

            // Parse main JOIN condition (table1.col = table2.col)
            var joinConditionPattern = @"(\w+)\.(\w+)\s*=\s*(\w+)\.(\w+)";
            var joinMatch = Regex.Match(onClause, joinConditionPattern);
            
            if (joinMatch.Success)
            {
                var alias1 = joinMatch.Groups[1].Value;
                var column1 = joinMatch.Groups[2].Value;
                var alias2 = joinMatch.Groups[3].Value;
                var column2 = joinMatch.Groups[4].Value;

                requirements.JoinRequirements.Add(new JoinRequirement
                {
                    LeftTableAlias = alias1,
                    LeftColumn = column1,
                    RightTableAlias = alias2,
                    RightColumn = column2,
                    RightTable = table
                });

                _logger.Information("Found JOIN: {LeftAlias}.{LeftCol} = {RightAlias}.{RightCol}",
                    alias1, column1, alias2, column2);
            }

            // Also extract any additional WHERE-like conditions in JOIN ON clause
            ParseEqualityConditions(onClause, requirements);
            ParseBooleanConditions(onClause, requirements);
            ParseComparisonConditions(onClause, requirements);
            ParseNullChecks(onClause, requirements);
        }
    }

    private static bool IsBooleanOrNumeric(string value)
    {
        var booleanNumericValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TRUE", "FALSE", "0", "1", "NULL"
        };
        
        return booleanNumericValues.Contains(value) || int.TryParse(value, out _) || decimal.TryParse(value, out _);
    }

    private static bool IsReservedWord(string word)
    {
        var reservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "OUTER", "ON",
            "AND", "OR", "NOT", "IN", "EXISTS", "LIKE", "BETWEEN", "IS", "NULL",
            "ORDER", "BY", "GROUP", "HAVING", "DISTINCT", "TOP", "LIMIT", "OFFSET",
            "TRUE", "FALSE", "NOW", "DATE_ADD", "YEAR", "INTERVAL", "DAY", "MONTH",
            "DATE", "TIME", "TIMESTAMP", "CURRENT_DATE", "CURRENT_TIME", "CURRENT_TIMESTAMP"
        };

        return reservedWords.Contains(word);
    }
}
