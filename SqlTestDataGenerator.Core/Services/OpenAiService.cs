using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using SqlTestDataGenerator.Core.Models;
using Serilog;
using Polly;

namespace SqlTestDataGenerator.Core.Services;

public class OpenAiService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public OpenAiService(string? apiKey = null)
    {
        _logger = Log.ForContext<OpenAiService>();
        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    public async Task<List<InsertStatement>> GenerateInsertStatementsAsync(
        DatabaseInfo databaseInfo, 
        string sqlQuery, 
        int desiredRecordCount,
        int currentRecordCount = 0)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Console.WriteLine("[OpenAiService] API key not configured, falling back to simple data generation");
            _logger.Warning("Gemini API key not configured, falling back to simple data generation");
            return new List<InsertStatement>();
        }

        try
        {
            var prompt = BuildPrompt(databaseInfo, sqlQuery, desiredRecordCount, currentRecordCount);
            Console.WriteLine($"[OpenAiService] Built prompt with {prompt.Length} chars for {databaseInfo.Tables.Count} tables (current: {currentRecordCount}, target: {desiredRecordCount})");
            _logger.Information("Sending request to Gemini with {TableCount} tables", databaseInfo.Tables.Count);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = GetSystemPrompt() + "\n\n" + prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 4000,
                    topP = 0.8,
                    topK = 40
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"[OpenAiService] Gemini API retry {retryCount} after {timespan.TotalMilliseconds}ms");
                        _logger.Warning("Gemini API retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                    });

            var geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
            Console.WriteLine($"[OpenAiService] Calling Gemini API...");

            var response = await retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _httpClient.PostAsync(geminiUrl, content);
                Console.WriteLine($"[OpenAiService] Gemini response status: {result.StatusCode}");
                result.EnsureSuccessStatusCode();
                return result;
            });

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[OpenAiService] Gemini response length: {responseContent.Length} chars");
            Console.WriteLine($"[OpenAiService] Full response: {responseContent}");
            _logger.Debug("Gemini Response: {Response}", responseContent);

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
            Console.WriteLine($"[OpenAiService] Candidates count: {geminiResponse?.Candidates?.Count ?? 0}");
            
            if (geminiResponse?.Candidates?.Any() == true)
            {
                var firstCandidate = geminiResponse.Candidates.First();
                Console.WriteLine($"[OpenAiService] First candidate content parts count: {firstCandidate?.Content?.Parts?.Count ?? 0}");
                
                if (firstCandidate?.Content?.Parts?.Any() == true)
                {
                    var firstPart = firstCandidate.Content.Parts.First();
                    Console.WriteLine($"[OpenAiService] First part text length: {firstPart?.Text?.Length ?? 0}");
                    Console.WriteLine($"[OpenAiService] First part text preview: {firstPart?.Text?.Substring(0, Math.Min(200, firstPart?.Text?.Length ?? 0))}");
                }
            }
            
            if (geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text == null)
            {
                Console.WriteLine("[OpenAiService] Invalid response format from Gemini - no text found");
                throw new InvalidOperationException("Invalid response format from Gemini");
            }

            var insertResponseContent = geminiResponse.Candidates.First().Content.Parts.First().Text;
            Console.WriteLine($"[OpenAiService] AI response text length: {insertResponseContent.Length} chars");
            
            // Clean up the response - Gemini wraps JSON in markdown code blocks
            // Look for ```json ... ``` pattern first
            var jsonMarkdownStart = insertResponseContent.IndexOf("```json");
            var jsonMarkdownEnd = insertResponseContent.IndexOf("```", jsonMarkdownStart + 7);
            
            if (jsonMarkdownStart >= 0 && jsonMarkdownEnd > jsonMarkdownStart)
            {
                // Extract JSON from markdown block
                var startPos = jsonMarkdownStart + 7; // Skip "```json"
                insertResponseContent = insertResponseContent.Substring(startPos, jsonMarkdownEnd - startPos).Trim();
                Console.WriteLine($"[OpenAiService] Extracted JSON from markdown block: {insertResponseContent.Length} chars");
            }
            else
            {
                // Fallback: Look for plain JSON brackets
                var jsonStart = insertResponseContent.IndexOf('{');
                var jsonEnd = insertResponseContent.LastIndexOf('}');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    insertResponseContent = insertResponseContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    Console.WriteLine($"[OpenAiService] Cleaned JSON length: {insertResponseContent.Length} chars");
                }
                else
                {
                    Console.WriteLine("[OpenAiService] Warning: Could not find JSON brackets in response");
                }
            }

            var insertData = JsonSerializer.Deserialize<OpenAiInsertResponse>(insertResponseContent, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            if (insertData?.Inserts == null)
            {
                Console.WriteLine("[OpenAiService] Invalid JSON response format from Gemini - no inserts array");
                throw new InvalidOperationException("Invalid JSON response format from Gemini");
            }

            Console.WriteLine($"[OpenAiService] Parsed {insertData.Inserts.Count} INSERT statements from AI");

            var insertStatements = new List<InsertStatement>();

            foreach (var insert in insertData.Inserts)
            {
                // Determine priority based on foreign key dependencies
                var table = databaseInfo.Tables.GetValueOrDefault(insert.Table);
                var tablePriority = table?.ForeignKeys.Count > 0 ? 1 : 0;

                insertStatements.Add(new InsertStatement
                {
                    TableName = insert.Table,
                    SqlStatement = insert.Sql,
                    Priority = tablePriority
                });
                
                Console.WriteLine($"[OpenAiService] Table: {insert.Table}, Priority: {tablePriority}");
            }

            _logger.Information("Generated {Count} INSERT statements from Gemini", insertStatements.Count);
            Console.WriteLine($"[OpenAiService] Successfully generated {insertStatements.Count} INSERT statements");
            return insertStatements;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OpenAiService] Failed to generate INSERT statements: {ex.Message}");
            Console.WriteLine($"[OpenAiService] Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[OpenAiService] Inner exception: {ex.InnerException.Message}");
            }
            _logger.Error(ex, "Failed to generate INSERT statements from Gemini");
            return new List<InsertStatement>();
        }
    }

    private static string GetSystemPrompt()
    {
        return @"You are a senior database developer expert in generating realistic test data for complex SQL queries with JOINs.

🎯 ENHANCED ACCURACY REQUIREMENTS:
1. ANALYZE the provided SQL query carefully to understand what data patterns it's looking for
2. EXAMINE the database schema to understand table relationships and constraints
3. GENERATE INSERT statements that will make the SQL query return meaningful results
4. CREATE realistic, consistent data that satisfies JOIN conditions and WHERE clauses
5. ENSURE foreign key relationships are properly maintained across all tables

🚨 CRITICAL DATA VALIDATION RULES:
✅ DATE GENERATION:
- Generate ONLY valid calendar dates (no February 30th, April 31st, etc.)
- Use format: 'YYYY-MM-DD' (e.g., '1989-05-15', '1989-11-28')
- For birth years like 1989: ensure month/day combinations exist (Feb 1-28, Apr 1-30, etc.)
- Test each date mentally: Does February 30th exist? NO! Use Feb 1-28 only

✅ CONSTRAINT MATCHING:
- LIKE '%VNEXT%' → company name must CONTAIN 'VNEXT' somewhere (e.g., 'VNEXT Software', 'ABC VNEXT Corp')
- first_name LIKE '%Phương%' → MUST contain 'Phương' (not just first_name = 'Phương')
- Boolean values: use 1/0 for database compatibility (not TRUE/FALSE strings)
- Join conditions: ensure foreign key values match exactly

✅ REALISTIC BUSINESS DATA:
- Vietnamese names: Phương, Hương, Minh, Anh, Thảo, Linh, Hải, Tuấn
- Company patterns: 'VNEXT Software', 'VNEXT Technology', 'ABC VNEXT Solutions'
- Roles: 'Developer', 'Manager', 'Analyst', 'Lead', 'Senior'
- Email format: firstname.lastname@company.com

Key Requirements:
- Study the SQL query's JOIN conditions, WHERE clauses, and expected result columns
- Generate data that specifically matches the query's filtering criteria
- Create enough base data so complex JOINs will return the desired number of rows
- Use realistic Vietnamese/English business data (companies, employees, roles, etc.)
- Maintain referential integrity between related tables
- Generate data in dependency order (parent tables before child tables)

Return your response as a JSON object with this exact structure:
{
  ""inserts"": [
    {
      ""table"": ""companies"",
      ""sql"": ""INSERT INTO companies (name, code) VALUES ('VNEXT Software', 'VNEXT001')""
    },
    {
      ""table"": ""users"", 
      ""sql"": ""INSERT INTO users (first_name, company_id, date_of_birth) VALUES ('Phương', 1, '1989-05-15')""
    }
  ]
}

🔒 STRICT GENERATION RULES:
- Generate only INSERT statements, no other SQL
- Skip identity/auto-increment columns (id, auto_increment fields) and GENERATED/COMPUTED columns
- Include ALL other columns that are NOT NULL
- Ensure foreign keys reference valid parent records
- Create data that specifically matches the SQL query's conditions
- For missing required columns (like username, password_hash), generate appropriate default values
- Use realistic Vietnamese names and business context when appropriate
- Return ONLY the JSON object, no markdown formatting or additional text

⚠️ VALIDATION CHECKLIST (Check each INSERT before generating):
□ Are all dates valid calendar dates? (No Feb 30, Apr 31, etc.)
□ Do LIKE pattern values actually contain the required text?
□ Do boolean values use 1/0 format?
□ Are foreign keys referencing valid parent records?
□ Are all NOT NULL columns included (except GENERATED)?

IMPORTANT: Always include ALL NOT NULL columns in your INSERT statements EXCEPT those marked as GENERATED/COMPUTED. For any required fields not mentioned in the query:
- username: generate from first_name + last_name (e.g., 'phuong.nguyen')
- password_hash: use placeholder like 'temp_hash_123'  
- email: generate realistic emails
- Other required fields: generate appropriate default values

CRITICAL: Do NOT include columns marked as GENERATED - these are auto-computed by the database (e.g., full_name, timestamps with DEFAULT_GENERATED).";
    }

    private static string BuildPrompt(DatabaseInfo databaseInfo, string sqlQuery, int desiredRecordCount, int currentRecordCount = 0)
    {
        var schemaInfo = BuildSchemaInfo(databaseInfo);
        var needsMoreRecords = desiredRecordCount - currentRecordCount;
        
        var contextInfo = currentRecordCount > 0 
            ? $"CURRENT SITUATION:\nThe database already has some data. When I run the target SQL query, it currently returns {currentRecordCount} rows.\nI need the query to return {desiredRecordCount} total rows, so I need to generate data that will result in {needsMoreRecords} additional rows.\n\n"
            : "CURRENT SITUATION:\nThis is a fresh start. The database may be empty or have minimal data.\n\n";
        
        return $@"DATABASE SCHEMA:
{schemaInfo}

{contextInfo}TARGET SQL QUERY TO ANALYZE:
{sqlQuery}

TASK REQUIREMENTS:
1. Carefully analyze the SQL query above - look at:
   - JOIN conditions between tables
   - WHERE clause filters and conditions
   - Column names being selected
   - Any date/name/company filters
   
2. Generate realistic INSERT statements that will ensure this SQL query returns {desiredRecordCount} total rows.
   {(currentRecordCount > 0 ? $"   IMPORTANT: The query currently returns {currentRecordCount} rows. Generate data for {needsMoreRecords} MORE rows." : "")}

3. Pay special attention to:
   - Business logic in the query (employee names, company names, role codes, dates)
   - Foreign key relationships that need to be satisfied for JOINs to work
   - Realistic data patterns (Vietnamese names if the query mentions them)
   - Generate diverse data to avoid duplicates

4. Create enough supporting data in parent tables so the JOINs will produce results.

Example: If query filters for 'Phương' names and 'VNEXT' companies, generate:
- Company records with 'VNEXT' in the name
- User records with 'Phương' in first_name/last_name
- Proper role assignments linking users to companies and roles

{(currentRecordCount > 0 ? $"RETRY STRATEGY: This is attempt to get more records. Previous attempts got {currentRecordCount} rows. Try different data patterns, names, dates, or companies to ensure variety and avoid conflicts." : "")}

Return only the JSON response with INSERT statements ordered by dependencies.";
    }

    private static string BuildSchemaInfo(DatabaseInfo databaseInfo)
    {
        var schema = new List<string>();

        foreach (var table in databaseInfo.Tables.Values)
        {
            var columns = new List<string>();
            
            foreach (var column in table.Columns)
            {
                var columnInfo = $"{column.ColumnName} {column.DataType}";
                
                if (column.IsPrimaryKey) columnInfo += " PK";
                if (column.IsIdentity) columnInfo += " IDENTITY";
                if (column.IsGenerated) columnInfo += " GENERATED";
                if (!column.IsNullable) columnInfo += " NOT NULL";
                if (column.MaxLength.HasValue) columnInfo += $"({column.MaxLength})";
                
                columns.Add(columnInfo);
            }

            var tableInfo = $"{table.TableName}({string.Join(", ", columns)})";
            
            if (table.ForeignKeys.Any())
            {
                var fks = table.ForeignKeys.Select(fk => 
                    $"{fk.ColumnName} -> {fk.ReferencedTable}.{fk.ReferencedColumn}");
                tableInfo += $" FK: [{string.Join(", ", fks)}]";
            }

            schema.Add(tableInfo);
        }

        return string.Join("\n", schema);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Gemini API Response Models
public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate> Candidates { get; set; } = new();
}

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent Content { get; set; } = new();
}

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = new();
}

public class GeminiPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

// Keep existing models for compatibility
public class OpenAiInsertResponse
{
    public List<OpenAiInsert> Inserts { get; set; } = new();
}

public class OpenAiInsert
{
    public string Table { get; set; } = string.Empty;
    public string Sql { get; set; } = string.Empty;
} 