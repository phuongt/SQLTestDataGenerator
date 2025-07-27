using SqlTestDataGenerator.Core.Abstractions;
using SqlTestDataGenerator.Core.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlTestDataGenerator.Core.Services
{
    /// <summary>
    /// MySQL-specific SQL dialect handler
    /// </summary>
    public class MySqlDialectHandler : ISqlDialectHandler
    {
        private static readonly HashSet<string> MySqlReservedWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "ALTER",
            "TABLE", "INDEX", "VIEW", "TRIGGER", "PROCEDURE", "FUNCTION", "EVENT", "SCHEMA",
            "DATABASE", "USER", "GRANT", "REVOKE", "COMMIT", "ROLLBACK", "START", "TRANSACTION",
            "SAVEPOINT", "LOCK", "UNLOCK", "SHOW", "DESCRIBE", "EXPLAIN", "USE", "SET", "RESET",
            "ORDER", "GROUP", "HAVING", "UNION", "INTERSECT", "EXCEPT", "ALL", "DISTINCT", "AS",
            "IN", "EXISTS", "BETWEEN", "LIKE", "IS", "AND", "OR", "NOT", "ANY", "SOME", "CASE",
            "WHEN", "THEN", "ELSE", "END", "IF", "IFNULL", "NULLIF", "COALESCE", "GREATEST", "LEAST"
        };

        public DatabaseType SupportedDatabaseType => DatabaseType.MySQL;

        /// <summary>
        /// Escape MySQL identifier
        /// </summary>
        public string EscapeIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return identifier;

            // MySQL supports backticks for identifiers
            if (NeedsQuoting(identifier))
            {
                return $"`{identifier}`";
            }

            return identifier;
        }

        /// <summary>
        /// Convert MySQL date functions (no conversion needed for MySQL)
        /// </summary>
        public string ConvertDateFunction(string mysqlFunction)
        {
            // MySQL functions are already in MySQL format
            return mysqlFunction;
        }

        /// <summary>
        /// Convert MySQL data types (no conversion needed for MySQL)
        /// </summary>
        public string ConvertDataType(string mysqlDataType)
        {
            // MySQL data types are already in MySQL format
            return mysqlDataType;
        }

        /// <summary>
        /// Format value for MySQL SQL
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
                case "INT":
                case "INTEGER":
                case "BIGINT":
                case "SMALLINT":
                case "TINYINT":
                case "DECIMAL":
                case "NUMERIC":
                case "FLOAT":
                case "DOUBLE":
                case "REAL":
                    if (decimal.TryParse(value, out _))
                    {
                        return value;
                    }
                    return "NULL";

                case "DATETIME":
                case "TIMESTAMP":
                    if (DateTime.TryParse(value, out var datetime))
                    {
                        return $"'{datetime:yyyy-MM-dd HH:mm:ss}'";
                    }
                    return "NULL";

                case "DATE":
                    if (DateTime.TryParse(value, out var date))
                    {
                        return $"'{date:yyyy-MM-dd}'";
                    }
                    return "NULL";

                case "TIME":
                    if (TimeSpan.TryParse(value, out var time))
                    {
                        return $"'{time:hh\\:mm\\:ss}'";
                    }
                    return "NULL";

                case "BOOLEAN":
                case "BOOL":
                case "BIT":
                    if (bool.TryParse(value, out var boolValue))
                    {
                        return boolValue ? "TRUE" : "FALSE";
                    }
                    if (value.Equals("1") || value.Equals("0"))
                    {
                        return value.Equals("1") ? "TRUE" : "FALSE";
                    }
                    return "FALSE";

                case "JSON":
                case "TEXT":
                case "LONGTEXT":
                case "MEDIUMTEXT":
                case "TINYTEXT":
                case "VARCHAR":
                case "CHAR":
                default:
                    return $"'{value.Replace("'", "''")}'";
            }
        }

        /// <summary>
        /// Get MySQL statement terminator
        /// </summary>
        public string GetStatementTerminator()
        {
            return ";"; // MySQL uses semicolons
        }

        /// <summary>
        /// Get MySQL pagination syntax
        /// </summary>
        public string GetPaginationSyntax(int offset, int limit)
        {
            return $"LIMIT {limit} OFFSET {offset}";
        }

        /// <summary>
        /// Get MySQL auto-increment syntax
        /// </summary>
        public string GetAutoIncrementSyntax(string tableName, string columnName)
        {
            return "AUTO_INCREMENT";
        }

        /// <summary>
        /// Check if identifier needs quoting
        /// </summary>
        private bool NeedsQuoting(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;

            // Check if it's a reserved word
            if (MySqlReservedWords.Contains(identifier))
                return true;

            // Check if it contains special characters or spaces
            if (Regex.IsMatch(identifier, @"[^A-Za-z0-9_$]"))
                return true;

            // Check if it starts with a number
            if (char.IsDigit(identifier[0]))
                return true;

            return false;
        }
    }
} 