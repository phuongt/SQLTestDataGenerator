using SqlTestDataGenerator.Core.Abstractions;
using SqlTestDataGenerator.Core.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlTestDataGenerator.Core.Services
{
    /// <summary>
    /// Oracle-specific SQL dialect handler
    /// </summary>
    public class OracleDialectHandler : ISqlDialectHandler
    {
        private static readonly HashSet<string> OracleReservedWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "ALTER",
            "TABLE", "INDEX", "VIEW", "SEQUENCE", "TRIGGER", "PROCEDURE", "FUNCTION", "PACKAGE",
            "BEGIN", "END", "DECLARE", "EXCEPTION", "CURSOR", "RECORD", "TYPE", "CONSTRAINT",
            "PRIMARY", "FOREIGN", "UNIQUE", "CHECK", "NOT", "NULL", "DEFAULT", "REFERENCES",
            "CASCADE", "SET", "RESTRICT", "GRANT", "REVOKE", "COMMIT", "ROLLBACK", "SAVEPOINT",
            "TRANSACTION", "SESSION", "USER", "SYSDATE", "SYSTIMESTAMP", "DUAL", "ROWNUM",
            "LEVEL", "CONNECT", "START", "PRIOR", "NOCYCLE", "NOCACHE", "ORDER", "GROUP",
            "HAVING", "UNION", "INTERSECT", "MINUS", "ALL", "DISTINCT", "AS", "IN", "EXISTS",
            "BETWEEN", "LIKE", "IS", "AND", "OR", "NOT", "ANY", "SOME", "CASE", "WHEN", "THEN",
            "ELSE", "DECODE", "NVL", "NVL2", "COALESCE", "NULLIF", "GREATEST", "LEAST"
        };

        public DatabaseType SupportedDatabaseType => DatabaseType.Oracle;

        /// <summary>
        /// Escape Oracle identifier
        /// </summary>
        public string EscapeIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return identifier;

            // Oracle supports unquoted identifiers in uppercase or lowercase
            // Mixed case, reserved words, or special characters need quotes
            if (NeedsQuoting(identifier))
            {
                return $"\"{identifier}\"";
            }

            return identifier;
        }

        /// <summary>
        /// Convert MySQL date functions to Oracle equivalents
        /// </summary>
        public string ConvertDateFunction(string mysqlFunction)
        {
            if (string.IsNullOrEmpty(mysqlFunction))
                return mysqlFunction;

            // Common MySQL to Oracle date function conversions
            var conversions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "NOW()", "SYSDATE" },
                { "CURDATE()", "TRUNC(SYSDATE)" },
                { "CURTIME()", "TO_CHAR(SYSDATE, 'HH24:MI:SS')" },
                { "CURRENT_TIMESTAMP", "SYSTIMESTAMP" },
                { "CURRENT_DATE", "TRUNC(SYSDATE)" },
                { "CURRENT_TIME", "TO_CHAR(SYSDATE, 'HH24:MI:SS')" }
            };

            if (conversions.TryGetValue(mysqlFunction, out var oracleEquivalent))
            {
                return oracleEquivalent;
            }

            // Handle DATE_ADD function
            var dateAddMatch = Regex.Match(mysqlFunction, @"DATE_ADD\s*\(\s*([^,]+)\s*,\s*INTERVAL\s+(\d+)\s+(\w+)\s*\)", RegexOptions.IgnoreCase);
            if (dateAddMatch.Success)
            {
                var dateExpr = dateAddMatch.Groups[1].Value.Trim();
                var interval = dateAddMatch.Groups[2].Value;
                var unit = dateAddMatch.Groups[3].Value.ToUpper();

                // Convert the date expression first
                var convertedDateExpr = ConvertDateFunction(dateExpr);
                return $"{convertedDateExpr} + {interval}";
            }

            // Handle DATE_SUB function
            var dateSubMatch = Regex.Match(mysqlFunction, @"DATE_SUB\s*\(\s*([^,]+)\s*,\s*INTERVAL\s+(\d+)\s+(\w+)\s*\)", RegexOptions.IgnoreCase);
            if (dateSubMatch.Success)
            {
                var dateExpr = dateSubMatch.Groups[1].Value.Trim();
                var interval = dateSubMatch.Groups[2].Value;
                var unit = dateSubMatch.Groups[3].Value.ToUpper();

                // Convert the date expression first
                var convertedDateExpr = ConvertDateFunction(dateExpr);
                return $"{convertedDateExpr} - {interval}";
            }

            // Handle YEAR, MONTH, DAY functions
            var yearMatch = Regex.Match(mysqlFunction, @"YEAR\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (yearMatch.Success)
            {
                return $"EXTRACT(YEAR FROM {yearMatch.Groups[1].Value})";
            }

            var monthMatch = Regex.Match(mysqlFunction, @"MONTH\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (monthMatch.Success)
            {
                return $"EXTRACT(MONTH FROM {monthMatch.Groups[1].Value})";
            }

            var dayMatch = Regex.Match(mysqlFunction, @"DAY\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            if (dayMatch.Success)
            {
                return $"EXTRACT(DAY FROM {dayMatch.Groups[1].Value})";
            }

            return mysqlFunction;
        }

        /// <summary>
        /// Convert MySQL data types to Oracle equivalents
        /// </summary>
        public string ConvertDataType(string mysqlDataType)
        {
            if (string.IsNullOrEmpty(mysqlDataType))
                return mysqlDataType;

            var conversions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "INT", "NUMBER" },
                { "INTEGER", "NUMBER" },
                { "BIGINT", "NUMBER" },
                { "SMALLINT", "NUMBER" },
                { "TINYINT", "NUMBER" },
                { "DECIMAL", "NUMBER" },
                { "NUMERIC", "NUMBER" },
                { "FLOAT", "NUMBER" },
                { "DOUBLE", "NUMBER" },
                { "REAL", "NUMBER" },
                { "VARCHAR", "VARCHAR2" },
                { "CHAR", "CHAR" },
                { "TEXT", "CLOB" },
                { "LONGTEXT", "CLOB" },
                { "MEDIUMTEXT", "CLOB" },
                { "TINYTEXT", "VARCHAR2(255)" },
                { "BLOB", "BLOB" },
                { "LONGBLOB", "BLOB" },
                { "MEDIUMBLOB", "BLOB" },
                { "TINYBLOB", "BLOB" },
                { "DATETIME", "TIMESTAMP" },
                { "TIMESTAMP", "TIMESTAMP" },
                { "DATE", "DATE" },
                { "TIME", "VARCHAR2(8)" },
                { "YEAR", "NUMBER(4)" },
                { "BOOLEAN", "NUMBER(1)" },
                { "BOOL", "NUMBER(1)" },
                { "BIT", "NUMBER(1)" },
                { "JSON", "CLOB" },
                { "ENUM", "VARCHAR2(50)" },
                { "SET", "VARCHAR2(255)" }
            };

            // Handle size specifications
            var sizeMatch = Regex.Match(mysqlDataType, @"(\w+)\s*\(\s*(\d+)\s*\)", RegexOptions.IgnoreCase);
            if (sizeMatch.Success)
            {
                var baseType = sizeMatch.Groups[1].Value;
                var size = sizeMatch.Groups[2].Value;

                if (conversions.TryGetValue(baseType, out var oracleType))
                {
                    if (oracleType == "NUMBER" || oracleType == "VARCHAR2" || oracleType == "CHAR")
                    {
                        return $"{oracleType}({size})";
                    }
                }
            }

            // Handle decimal precision
            var decimalMatch = Regex.Match(mysqlDataType, @"(\w+)\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)", RegexOptions.IgnoreCase);
            if (decimalMatch.Success)
            {
                var baseType = decimalMatch.Groups[1].Value;
                var precision = decimalMatch.Groups[2].Value;
                var scale = decimalMatch.Groups[3].Value;

                if (conversions.TryGetValue(baseType, out var oracleType))
                {
                    if (oracleType == "NUMBER")
                    {
                        return $"{oracleType}({precision},{scale})";
                    }
                }
            }

            // Simple type conversion
            if (conversions.TryGetValue(mysqlDataType, out var convertedType))
            {
                return convertedType;
            }

            return mysqlDataType;
        }

        /// <summary>
        /// Format value for Oracle SQL
        /// </summary>
        public string FormatValue(string value, string dataType)
        {
            if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return "NULL";
            }

            if (string.IsNullOrEmpty(dataType))
            {
                return $"'{value.Replace("'", "''")}'";
            }

            var normalizedType = dataType.ToUpper();
            
            // Handle different data types
            switch (normalizedType)
            {
                case "NUMBER":
                case "INTEGER":
                case "FLOAT":
                case "DOUBLE":
                    if (decimal.TryParse(value, out _))
                    {
                        return value;
                    }
                    return "NULL";

                case "DATE":
                    if (DateTime.TryParse(value, out var date))
                    {
                        return $"TO_DATE('{date:yyyy-MM-dd}', 'YYYY-MM-DD')";
                    }
                    return "NULL";

                case "TIMESTAMP":
                    if (DateTime.TryParse(value, out var timestamp))
                    {
                        return $"TO_TIMESTAMP('{timestamp:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";
                    }
                    return "NULL";

                case "NUMBER(1)": // Boolean equivalent
                    if (bool.TryParse(value, out var boolValue))
                    {
                        return boolValue ? "1" : "0";
                    }
                    if (value.Equals("1") || value.Equals("0"))
                    {
                        return value;
                    }
                    return "0";

                case "CLOB":
                case "VARCHAR2":
                case "CHAR":
                default:
                    return $"'{value.Replace("'", "''")}'";
            }
        }

        /// <summary>
        /// Get Oracle statement terminator
        /// </summary>
        public string GetStatementTerminator()
        {
            return ""; // Oracle doesn't use semicolons in ExecuteNonQueryAsync
        }

        /// <summary>
        /// Get Oracle pagination syntax
        /// </summary>
        public string GetPaginationSyntax(int offset, int limit)
        {
            if (offset == 0)
            {
                return $"WHERE ROWNUM <= {limit}";
            }
            else
            {
                return $"WHERE ROWNUM <= {offset + limit} AND ROWNUM > {offset}";
            }
        }

        /// <summary>
        /// Get Oracle auto-increment syntax
        /// </summary>
        public string GetAutoIncrementSyntax(string tableName, string columnName)
        {
            return $"{tableName}_seq.NEXTVAL";
        }

        /// <summary>
        /// Check if identifier needs quoting
        /// </summary>
        private bool NeedsQuoting(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;

            // Check if it's a reserved word
            if (OracleReservedWords.Contains(identifier))
                return true;

            // Check if it contains special characters or spaces
            if (Regex.IsMatch(identifier, @"[^A-Za-z0-9_$#]"))
                return true;

            // Check if it starts with a number
            if (char.IsDigit(identifier[0]))
                return true;

            // Check if it's mixed case (Oracle convention)
            if (identifier != identifier.ToUpper() && identifier != identifier.ToLower())
                return true;

            return false;
        }

        /// <summary>
        /// Convert MySQL interval units to Oracle equivalents
        /// </summary>
        private string ConvertIntervalUnit(string mysqlUnit)
        {
            var conversions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "YEAR", "YEAR" },
                { "MONTH", "MONTH" },
                { "DAY", "DAY" },
                { "HOUR", "HOUR" },
                { "MINUTE", "MINUTE" },
                { "SECOND", "SECOND" },
                { "WEEK", "DAY" }, // Oracle doesn't have WEEK, use DAY * 7
                { "QUARTER", "MONTH" } // Oracle doesn't have QUARTER, use MONTH * 3
            };

            return conversions.TryGetValue(mysqlUnit, out var oracleUnit) ? oracleUnit : "DAY";
        }
    }
} 