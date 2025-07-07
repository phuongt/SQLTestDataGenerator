using SqlTestDataGenerator.Core.Models;
using System.Data;
using System.Diagnostics;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Interface for data generation services
/// </summary>
public interface IDataGenerator
{
    /// <summary>
    /// Generate test data based on SQL template
    /// </summary>
    ExecutionResult GenerateData(SqlTemplate template, GenerationSettings settings);

    /// <summary>
    /// Generate test data asynchronously
    /// </summary>
    Task<ExecutionResult> GenerateDataAsync(SqlTemplate template, GenerationSettings settings);

    /// <summary>
    /// Generate data for multiple templates
    /// </summary>
    Task<List<ExecutionResult>> GenerateMultipleAsync(List<SqlTemplate> templates, GenerationSettings settings);

    /// <summary>
    /// Preview generated data without execution
    /// </summary>
    List<string> PreviewData(SqlTemplate template, GenerationSettings settings);

    /// <summary>
    /// Validate data generation settings
    /// </summary>
    ValidationResult ValidateSettings(GenerationSettings settings);
}

/// <summary>
/// Main data generator service with comprehensive logging and validation
/// </summary>
public class DataGenerator : IDataGenerator
{
    private readonly ILoggerService _logger;
    private readonly ISqlTemplateParser _templateParser;
    private readonly IDbExecutor _dbExecutor;
    private readonly Random _random;

    public DataGenerator(ILoggerService logger, ISqlTemplateParser templateParser, IDbExecutor dbExecutor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templateParser = templateParser ?? throw new ArgumentNullException(nameof(templateParser));
        _dbExecutor = dbExecutor ?? throw new ArgumentNullException(nameof(dbExecutor));
        _random = new Random();

        _logger.LogMethodEntry(nameof(DataGenerator));
        _logger.LogMethodExit(nameof(DataGenerator));
    }

    public ExecutionResult GenerateData(SqlTemplate template, GenerationSettings settings)
    {
        _logger.LogMethodEntry(nameof(GenerateData), new { templateId = template.Id, settings.RecordCount });

        var stopwatch = Stopwatch.StartNew();
        var result = new ExecutionResult();
        
        try
        {
            // Validate input parameters
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _logger.LogInfo($"Starting data generation for template: {template.Name}", nameof(GenerateData));

            // Step 1: Validate settings
            var validationResult = ValidateSettings(settings);
            if (!validationResult.IsValid)
            {
                result.Success = false;
                result.ErrorMessage = "Invalid generation settings";
                result.ValidationMessages = validationResult.Messages;
                _logger.LogError("Generation settings validation failed", null, nameof(GenerateData), validationResult.Messages);
                return result;
            }

            // Step 2: Validate template
            var templateValidation = _templateParser.ValidateTemplate(template);
            if (!templateValidation.IsValid)
            {
                result.Success = false;
                result.ErrorMessage = "Invalid template";
                result.ValidationMessages = templateValidation.Messages;
                _logger.LogError("Template validation failed", null, nameof(GenerateData), templateValidation.Messages);
                return result;
            }

            // Step 3: Generate SQL statements
            _logger.LogInfo($"Generating {settings.RecordCount} SQL statements from template", nameof(GenerateData));
            var generatedSql = _templateParser.GenerateMultiple(template, settings, settings.RecordCount);
            result.GeneratedSql = generatedSql;
            result.StatementsExecuted = generatedSql.Count;

            _logger.LogInfo($"Generated {generatedSql.Count} SQL statements", nameof(GenerateData));

            // Step 4: Execute SQL if requested
            if (!string.IsNullOrEmpty(settings.ConnectionString))
            {
                _logger.LogInfo("Executing generated SQL statements", nameof(GenerateData));
                var executionResult = _dbExecutor.ExecuteStatements(generatedSql, settings);
                
                if (!executionResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = executionResult.ErrorMessage;
                    result.Exception = executionResult.Exception;
                    _logger.LogError($"SQL execution failed: {executionResult.ErrorMessage}", executionResult.Exception, nameof(GenerateData));
                    return result;
                }

                result.ResultData = executionResult.ResultData;
                result.RecordsGenerated = executionResult.RecordsGenerated;
                _logger.LogInfo($"Successfully executed {result.StatementsExecuted} statements, generated {result.RecordsGenerated} records", nameof(GenerateData));
            }
            else
            {
                result.RecordsGenerated = generatedSql.Count;
                _logger.LogInfo("SQL generation completed without execution (no connection string provided)", nameof(GenerateData));
            }

            // Step 5: Record performance metrics
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
            result.Performance.DataGenerationTime = stopwatch.Elapsed;
            result.Performance.RecordsPerSecond = result.RecordsGenerated / Math.Max(0.001, stopwatch.Elapsed.TotalSeconds);

            result.Success = true;
            _logger.LogInfo($"Data generation completed successfully in {stopwatch.ElapsedMilliseconds}ms", nameof(GenerateData));
            _logger.LogMethodExit(nameof(GenerateData), new { success = result.Success, recordsGenerated = result.RecordsGenerated });

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
            result.ExecutionTime = stopwatch.Elapsed;

            _logger.LogError($"Data generation failed: {ex.Message}", ex, nameof(GenerateData));
            _logger.LogMethodExit(nameof(GenerateData), new { success = false, error = ex.Message });

            return result;
        }
    }

    public async Task<ExecutionResult> GenerateDataAsync(SqlTemplate template, GenerationSettings settings)
    {
        _logger.LogMethodEntry(nameof(GenerateDataAsync), new { templateId = template.Id, settings.RecordCount });

        try
        {
            var result = await Task.Run(() => GenerateData(template, settings));
            _logger.LogMethodExit(nameof(GenerateDataAsync), new { success = result.Success, recordsGenerated = result.RecordsGenerated });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Async data generation failed: {ex.Message}", ex, nameof(GenerateDataAsync));
            _logger.LogMethodExit(nameof(GenerateDataAsync), new { success = false, error = ex.Message });
            throw;
        }
    }

    public async Task<List<ExecutionResult>> GenerateMultipleAsync(List<SqlTemplate> templates, GenerationSettings settings)
    {
        _logger.LogMethodEntry(nameof(GenerateMultipleAsync), new { templateCount = templates.Count, settings.RecordCount });

        try
        {
            var results = new List<ExecutionResult>();
            var maxParallel = Math.Min(settings.Performance.MaxParallelOperations, templates.Count);

            _logger.LogInfo($"Processing {templates.Count} templates with max {maxParallel} parallel operations", nameof(GenerateMultipleAsync));

            // Process templates in parallel with controlled concurrency
            var semaphore = new SemaphoreSlim(maxParallel, maxParallel);
            var tasks = templates.Select(async template =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await GenerateDataAsync(template, settings);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var allResults = await Task.WhenAll(tasks);
            results.AddRange(allResults);

            var successCount = results.Count(r => r.Success);
            var totalRecords = results.Sum(r => r.RecordsGenerated);

            _logger.LogInfo($"Multiple template generation completed: {successCount}/{templates.Count} succeeded, {totalRecords} total records", nameof(GenerateMultipleAsync));
            _logger.LogMethodExit(nameof(GenerateMultipleAsync), new { templateCount = templates.Count, successCount, totalRecords });

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Multiple template generation failed: {ex.Message}", ex, nameof(GenerateMultipleAsync));
            _logger.LogMethodExit(nameof(GenerateMultipleAsync), new { templateCount = templates.Count, success = false, error = ex.Message });
            throw;
        }
    }

    public List<string> PreviewData(SqlTemplate template, GenerationSettings settings)
    {
        _logger.LogMethodEntry(nameof(PreviewData), new { templateId = template.Id, previewCount = Math.Min(settings.RecordCount, 10) });

        try
        {
            // Limit preview to reasonable number
            var previewCount = Math.Min(settings.RecordCount, 10);
            var previewSettings = new GenerationSettings
            {
                RecordCount = previewCount,
                DatabaseType = settings.DatabaseType,
                UseAI = settings.UseAI,
                OpenAiApiKey = settings.OpenAiApiKey,
                Locale = settings.Locale,
                RandomSeed = settings.RandomSeed,
                PlaceholderParameters = settings.PlaceholderParameters,
                DataPatterns = settings.DataPatterns
            };

            var preview = _templateParser.GenerateMultiple(template, previewSettings, previewCount);
            _logger.LogInfo($"Generated preview with {preview.Count} statements", nameof(PreviewData));
            _logger.LogMethodExit(nameof(PreviewData), new { previewCount = preview.Count });

            return preview;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Preview generation failed: {ex.Message}", ex, nameof(PreviewData));
            _logger.LogMethodExit(nameof(PreviewData), new { success = false, error = ex.Message });
            throw;
        }
    }

    public ValidationResult ValidateSettings(GenerationSettings settings)
    {
        _logger.LogMethodEntry(nameof(ValidateSettings), new { settings });

        try
        {
            var result = new ValidationResult();

            if (settings == null)
            {
                result.IsValid = false;
                result.Messages.Add(new ValidationMessage
                {
                    Severity = ValidationSeverity.Error,
                    Message = "Generation settings cannot be null"
                });
                return result;
            }

            // Validate record count
            if (settings.RecordCount <= 0)
            {
                result.Messages.Add(new ValidationMessage
                {
                    Severity = ValidationSeverity.Error,
                    Message = "Record count must be greater than 0"
                });
                result.IsValid = false;
            }

            if (settings.RecordCount > 10000)
            {
                result.Messages.Add(new ValidationMessage
                {
                    Severity = ValidationSeverity.Warning,
                    Message = "Large record count may impact performance"
                });
            }

            // Validate database type
            var supportedDatabaseTypes = new[] { "MySQL", "PostgreSQL", "SQL Server", "SQLite" };
            if (!supportedDatabaseTypes.Contains(settings.DatabaseType, StringComparer.OrdinalIgnoreCase))
            {
                result.Messages.Add(new ValidationMessage
                {
                    Severity = ValidationSeverity.Error,
                    Message = $"Database type '{settings.DatabaseType}' is not supported. Supported types: {string.Join(", ", supportedDatabaseTypes)}"
                });
                result.IsValid = false;
            }

            // Validate AI settings
            if (settings.UseAI && string.IsNullOrEmpty(settings.OpenAiApiKey))
            {
                result.Messages.Add(new ValidationMessage
                {
                    Severity = ValidationSeverity.Warning,
                    Message = "AI generation is enabled but no API key is provided"
                });
            }

            // Validate connection string if provided
            if (!string.IsNullOrEmpty(settings.ConnectionString))
            {
                if (settings.ConnectionString.Length < 10)
                {
                    result.Messages.Add(new ValidationMessage
                    {
                        Severity = ValidationSeverity.Warning,
                        Message = "Connection string appears to be too short"
                    });
                }
            }

            // Validate performance settings
            if (settings.Performance.MaxParallelOperations <= 0)
            {
                result.Messages.Add(new ValidationMessage
                {
                    Severity = ValidationSeverity.Warning,
                    Message = "Max parallel operations should be greater than 0"
                });
            }

            if (settings.Performance.MemoryLimitMB <= 0)
            {
                result.Messages.Add(new ValidationMessage
                {
                    Severity = ValidationSeverity.Warning,
                    Message = "Memory limit should be greater than 0"
                });
            }

            _logger.LogInfo($"Settings validation completed. IsValid: {result.IsValid}, Messages: {result.Messages.Count}", nameof(ValidateSettings));
            _logger.LogMethodExit(nameof(ValidateSettings), new { isValid = result.IsValid, messageCount = result.Messages.Count });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Settings validation failed: {ex.Message}", ex, nameof(ValidateSettings));
            _logger.LogMethodExit(nameof(ValidateSettings), new { success = false, error = ex.Message });
            throw;
        }
    }
}

/// <summary>
/// Validation result for data generation operations
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the validation passed
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// List of validation messages
    /// </summary>
    public List<ValidationMessage> Messages { get; set; } = new();

    /// <summary>
    /// Additional validation metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}