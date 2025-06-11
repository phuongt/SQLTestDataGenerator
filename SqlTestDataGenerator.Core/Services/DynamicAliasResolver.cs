using System.Text.RegularExpressions;
using SqlTestDataGenerator.Core.Models;
using Serilog;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Dynamic resolver for table aliases in SQL queries.
/// Replaces hardcoded alias mappings with runtime detection.
/// </summary>
public class DynamicAliasResolver
{
    private readonly ILogger _logger;

    public DynamicAliasResolver()
    {
        _logger = Log.ForContext<DynamicAliasResolver>();
    }

    /// <summary>
    /// Extract table alias mapping from SQL query dynamically
    /// </summary>
    /// <param name="sqlQuery">SQL query to analyze</param>
    /// <param name="databaseInfo">Database schema info (optional)</param>
    /// <returns>Dictionary mapping alias to table name</returns>
    public Dictionary<string, string> ExtractAliasMapping(string sqlQuery, DatabaseInfo? databaseInfo = null)
    {
        _logger.Information("Extracting alias mapping from SQL query");
        
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            // 1. Extract FROM clause aliases: FROM table_name alias
            ExtractFromClauseAliases(sqlQuery, mapping);
            
            // 2. Extract JOIN clause aliases: JOIN table_name alias ON...
            ExtractJoinClauseAliases(sqlQuery, mapping);
            
            // 3. Apply fallback pattern matching if needed
            ApplyFallbackPatternMatching(mapping, databaseInfo);
            
            _logger.Information("Extracted {Count} alias mappings: {Mappings}", 
                mapping.Count, string.Join(", ", mapping.Select(kvp => $"{kvp.Key}→{kvp.Value}")));
            
            return mapping;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to extract alias mapping from SQL query");
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Extract aliases from FROM clause
    /// Pattern: FROM table_name alias, FROM `table_name` alias
    /// </summary>
    private void ExtractFromClauseAliases(string sqlQuery, Dictionary<string, string> mapping)
    {
        // Pattern supports optional backticks and whitespace variations
        var fromPattern = @"FROM\s+`?(\w+)`?\s+(\w+)(?=\s|$|WHERE|JOIN|ORDER|GROUP|HAVING|LIMIT)";
        var fromMatches = Regex.Matches(sqlQuery, fromPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in fromMatches)
        {
            var tableName = match.Groups[1].Value;
            var alias = match.Groups[2].Value;
            
            // Skip if alias is a SQL keyword
            if (!IsSqlKeyword(alias))
            {
                mapping[alias] = tableName;
                _logger.Debug("FROM clause: {Alias} → {Table}", alias, tableName);
            }
        }
    }

    /// <summary>
    /// Extract aliases from JOIN clauses
    /// Pattern: JOIN table_name alias ON..., INNER JOIN table_name alias ON...
    /// </summary>
    private void ExtractJoinClauseAliases(string sqlQuery, Dictionary<string, string> mapping)
    {
        // Pattern supports different JOIN types and optional backticks
        var joinPattern = @"(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+)?JOIN\s+`?(\w+)`?\s+(\w+)\s+ON";
        var joinMatches = Regex.Matches(sqlQuery, joinPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in joinMatches)
        {
            var tableName = match.Groups[1].Value;
            var alias = match.Groups[2].Value;
            
            // Skip if alias is a SQL keyword
            if (!IsSqlKeyword(alias))
            {
                mapping[alias] = tableName;
                _logger.Debug("JOIN clause: {Alias} → {Table}", alias, tableName);
            }
        }
    }

    /// <summary>
    /// Apply fallback pattern matching for common alias conventions
    /// Only used when explicit alias mapping is not found
    /// </summary>
    private void ApplyFallbackPatternMatching(Dictionary<string, string> mapping, DatabaseInfo? databaseInfo)
    {
        if (databaseInfo?.Tables == null) return;

        var allTableNames = databaseInfo.Tables.Values.Select(t => t.TableName).ToList();
        
        // Find unmapped aliases that might follow common patterns
        var potentialAliases = ExtractPotentialAliases(mapping.Keys.ToList());
        
        foreach (var alias in potentialAliases)
        {
            if (mapping.ContainsKey(alias)) continue; // Already mapped
            
            var matchedTable = FindTableByPattern(alias, allTableNames);
            if (matchedTable != null)
            {
                mapping[alias] = matchedTable;
                _logger.Debug("Pattern fallback: {Alias} → {Table}", alias, matchedTable);
            }
        }
    }

    /// <summary>
    /// Extract potential aliases from WHERE/SELECT clauses that might not be in FROM/JOIN
    /// </summary>
    private List<string> ExtractPotentialAliases(List<string> knownAliases)
    {
        // This could be extended to parse SELECT and WHERE clauses for additional aliases
        // For now, return known aliases to avoid over-engineering
        return knownAliases;
    }

    /// <summary>
    /// Find table name by pattern matching alias
    /// Common patterns: u → users, c → companies, ur → user_roles
    /// </summary>
    private string? FindTableByPattern(string alias, List<string> tableNames)
    {
        var aliasLower = alias.ToLower();
        
        // 1. Exact match
        var exactMatch = tableNames.FirstOrDefault(t => t.Equals(alias, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null) return exactMatch;
        
        // 2. Prefix match (u → users, c → companies)
        var prefixMatch = tableNames.FirstOrDefault(t => 
            t.ToLower().StartsWith(aliasLower) && aliasLower.Length <= 3);
        if (prefixMatch != null) return prefixMatch;
        
        // 3. Acronym match (ur → user_roles)
        var acronymMatch = tableNames.FirstOrDefault(t => 
            GenerateAcronym(t).Equals(aliasLower, StringComparison.OrdinalIgnoreCase));
        if (acronymMatch != null) return acronymMatch;
        
        // 4. Contains match (as last resort)
        var containsMatch = tableNames.FirstOrDefault(t => 
            t.ToLower().Contains(aliasLower) || aliasLower.Contains(t.ToLower()));
        if (containsMatch != null) return containsMatch;
        
        return null;
    }

    /// <summary>
    /// Generate acronym from table name (user_roles → ur)
    /// </summary>
    private string GenerateAcronym(string tableName)
    {
        // Split by underscore and take first letter of each part
        var parts = tableName.Split('_', '-');
        if (parts.Length > 1)
        {
            return string.Join("", parts.Select(p => p.FirstOrDefault())).ToLower();
        }
        
        // For single words, take first few characters
        return tableName.Length >= 2 ? tableName.Substring(0, 2).ToLower() : tableName.ToLower();
    }

    /// <summary>
    /// Check if string is a SQL keyword
    /// </summary>
    private bool IsSqlKeyword(string word)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER",
            "ON", "AND", "OR", "NOT", "NULL", "TRUE", "FALSE", "ORDER", "BY", "GROUP",
            "HAVING", "LIMIT", "OFFSET", "UNION", "ALL", "DISTINCT", "AS", "CASE", "WHEN",
            "THEN", "ELSE", "END", "IN", "EXISTS", "BETWEEN", "LIKE", "IS", "ASC", "DESC"
        };
        
        return keywords.Contains(word);
    }

    /// <summary>
    /// Resolve table name from alias using the mapping
    /// </summary>
    /// <param name="alias">Table alias</param>
    /// <param name="mapping">Alias to table mapping</param>
    /// <returns>Resolved table name or original alias if not found</returns>
    public string ResolveTableName(string alias, Dictionary<string, string> mapping)
    {
        if (string.IsNullOrEmpty(alias)) return alias;
        
        return mapping.GetValueOrDefault(alias, alias);
    }

    /// <summary>
    /// Check if alias matches table name using the mapping
    /// </summary>
    /// <param name="alias">Table alias from constraint</param>
    /// <param name="tableName">Actual table name</param>
    /// <param name="mapping">Alias mapping dictionary</param>
    /// <returns>True if alias matches table</returns>
    public bool MatchesTable(string alias, string tableName, Dictionary<string, string> mapping)
    {
        if (string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(tableName))
            return false;
            
        // Direct table name match
        if (tableName.Equals(alias, StringComparison.OrdinalIgnoreCase))
            return true;
            
        // Resolve alias and compare
        var resolvedTableName = ResolveTableName(alias, mapping);
        return tableName.Equals(resolvedTableName, StringComparison.OrdinalIgnoreCase);
    }
} 