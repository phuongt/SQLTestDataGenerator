using SqlTestDataGenerator.Core.Models;
using System.Text.Json;

namespace SqlTestDataGenerator.Core.Services;

/// <summary>
/// Interface for MCP Interactive Feedback functionality
/// </summary>
public interface IInteractiveFeedbackService
{
    /// <summary>
    /// Show interactive feedback to user before completion
    /// </summary>
    Task<FeedbackResponse> ShowInteractiveFeedbackAsync(FeedbackRequest request);

    /// <summary>
    /// Ask user a question and wait for response
    /// </summary>
    Task<string> AskQuestionAsync(string question, List<string>? options = null);

    /// <summary>
    /// Get user feedback on a specific action or result
    /// </summary>
    Task<UserFeedback> GetUserFeedbackAsync(string context, object? data = null);

    /// <summary>
    /// Validate user input before proceeding
    /// </summary>
    Task<ValidationResponse> ValidateUserInputAsync(string input, ValidationRequest request);

    /// <summary>
    /// Show progress and allow user to cancel or modify ongoing operations
    /// </summary>
    Task<ProgressResponse> ShowProgressFeedbackAsync(ProgressInfo progress);
}

/// <summary>
/// MCP Interactive Feedback service implementation
/// </summary>
public class InteractiveFeedbackService : IInteractiveFeedbackService
{
    private readonly ILoggerService _logger;
    private readonly Queue<FeedbackRequest> _pendingRequests;
    private readonly object _lockObject = new();

    public InteractiveFeedbackService(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pendingRequests = new Queue<FeedbackRequest>();

        _logger.LogMethodEntry(nameof(InteractiveFeedbackService));
        _logger.LogMethodExit(nameof(InteractiveFeedbackService));
    }

    public async Task<FeedbackResponse> ShowInteractiveFeedbackAsync(FeedbackRequest request)
    {
        _logger.LogMethodEntry(nameof(ShowInteractiveFeedbackAsync), request);

        try
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Queue the request for processing
            lock (_lockObject)
            {
                _pendingRequests.Enqueue(request);
            }

            _logger.LogInfo($"Interactive feedback request queued: {request.Type}", nameof(ShowInteractiveFeedbackAsync));

            // Process the feedback request
            var response = await ProcessFeedbackRequestAsync(request);

            _logger.LogInfo($"Interactive feedback completed: {response.Status}", nameof(ShowInteractiveFeedbackAsync));
            _logger.LogMethodExit(nameof(ShowInteractiveFeedbackAsync), response);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Interactive feedback failed: {ex.Message}", ex, nameof(ShowInteractiveFeedbackAsync));
            _logger.LogMethodExit(nameof(ShowInteractiveFeedbackAsync), new { success = false, error = ex.Message });
            throw;
        }
    }

    public async Task<string> AskQuestionAsync(string question, List<string>? options = null)
    {
        _logger.LogMethodEntry(nameof(AskQuestionAsync), new { question, optionCount = options?.Count ?? 0 });

        try
        {
            var request = new FeedbackRequest
            {
                Id = Guid.NewGuid().ToString(),
                Type = FeedbackType.Question,
                Title = "User Input Required",
                Message = question,
                Options = options ?? new List<string>(),
                Timestamp = DateTime.Now,
                RequiresResponse = true
            };

            var response = await ShowInteractiveFeedbackAsync(request);

            var answer = response.UserResponse ?? string.Empty;
            _logger.LogInfo($"User answered question: {answer}", nameof(AskQuestionAsync));
            _logger.LogMethodExit(nameof(AskQuestionAsync), new { answer });

            return answer;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Question asking failed: {ex.Message}", ex, nameof(AskQuestionAsync));
            _logger.LogMethodExit(nameof(AskQuestionAsync), new { success = false, error = ex.Message });
            throw;
        }
    }

    public async Task<UserFeedback> GetUserFeedbackAsync(string context, object? data = null)
    {
        _logger.LogMethodEntry(nameof(GetUserFeedbackAsync), new { context, hasData = data != null });

        try
        {
            var request = new FeedbackRequest
            {
                Id = Guid.NewGuid().ToString(),
                Type = FeedbackType.Feedback,
                Title = "Feedback Required",
                Message = $"Please provide feedback for: {context}",
                Data = data,
                Timestamp = DateTime.Now,
                RequiresResponse = true
            };

            var response = await ShowInteractiveFeedbackAsync(request);

            var feedback = new UserFeedback
            {
                Context = context,
                Response = response.UserResponse ?? string.Empty,
                Rating = response.Rating,
                Comments = response.Comments,
                Timestamp = DateTime.Now,
                IsPositive = response.Rating >= 3 // Assuming 1-5 scale
            };

            _logger.LogInfo($"User feedback received: Rating={feedback.Rating}, Positive={feedback.IsPositive}", nameof(GetUserFeedbackAsync));
            _logger.LogMethodExit(nameof(GetUserFeedbackAsync), feedback);

            return feedback;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Getting user feedback failed: {ex.Message}", ex, nameof(GetUserFeedbackAsync));
            _logger.LogMethodExit(nameof(GetUserFeedbackAsync), new { success = false, error = ex.Message });
            throw;
        }
    }

    public async Task<ValidationResponse> ValidateUserInputAsync(string input, ValidationRequest request)
    {
        _logger.LogMethodEntry(nameof(ValidateUserInputAsync), new { input = input?.Substring(0, Math.Min(50, input?.Length ?? 0)), request });

        try
        {
            var validationResponse = new ValidationResponse
            {
                IsValid = true,
                Messages = new List<string>(),
                SuggestedCorrections = new List<string>(),
                ProcessedInput = input
            };

            // Apply validation rules
            if (request.ValidationRules != null)
            {
                foreach (var rule in request.ValidationRules)
                {
                    var ruleResult = await ApplyValidationRuleAsync(input, rule);
                    if (!ruleResult.IsValid)
                    {
                        validationResponse.IsValid = false;
                        validationResponse.Messages.AddRange(ruleResult.Messages);
                        validationResponse.SuggestedCorrections.AddRange(ruleResult.SuggestedCorrections);
                    }
                }
            }

            // Interactive validation if needed
            if (!validationResponse.IsValid && request.InteractiveValidation)
            {
                var feedbackRequest = new FeedbackRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = FeedbackType.Validation,
                    Title = "Input Validation Failed",
                    Message = $"The input '{input}' failed validation. Please review and correct:\n" + string.Join("\n", validationResponse.Messages),
                    Data = new { input, validationErrors = validationResponse.Messages },
                    Timestamp = DateTime.Now,
                    RequiresResponse = true
                };

                var feedbackResponse = await ShowInteractiveFeedbackAsync(feedbackRequest);
                if (!string.IsNullOrEmpty(feedbackResponse.UserResponse))
                {
                    validationResponse.ProcessedInput = feedbackResponse.UserResponse;
                    // Re-validate the corrected input
                    return await ValidateUserInputAsync(feedbackResponse.UserResponse, request);
                }
            }

            _logger.LogInfo($"Input validation completed: IsValid={validationResponse.IsValid}", nameof(ValidateUserInputAsync));
            _logger.LogMethodExit(nameof(ValidateUserInputAsync), validationResponse);

            return validationResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Input validation failed: {ex.Message}", ex, nameof(ValidateUserInputAsync));
            _logger.LogMethodExit(nameof(ValidateUserInputAsync), new { success = false, error = ex.Message });
            throw;
        }
    }

    public async Task<ProgressResponse> ShowProgressFeedbackAsync(ProgressInfo progress)
    {
        _logger.LogMethodEntry(nameof(ShowProgressFeedbackAsync), progress);

        try
        {
            var request = new FeedbackRequest
            {
                Id = Guid.NewGuid().ToString(),
                Type = FeedbackType.Progress,
                Title = progress.Title,
                Message = progress.Description,
                Data = progress,
                Timestamp = DateTime.Now,
                RequiresResponse = false
            };

            var feedbackResponse = await ShowInteractiveFeedbackAsync(request);

            var progressResponse = new ProgressResponse
            {
                ShouldContinue = feedbackResponse.Status != FeedbackStatus.Cancelled,
                UserAction = feedbackResponse.UserAction,
                ModifiedParameters = feedbackResponse.ModifiedParameters,
                Feedback = feedbackResponse.Comments
            };

            _logger.LogInfo($"Progress feedback processed: ShouldContinue={progressResponse.ShouldContinue}", nameof(ShowProgressFeedbackAsync));
            _logger.LogMethodExit(nameof(ShowProgressFeedbackAsync), progressResponse);

            return progressResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Progress feedback failed: {ex.Message}", ex, nameof(ShowProgressFeedbackAsync));
            _logger.LogMethodExit(nameof(ShowProgressFeedbackAsync), new { success = false, error = ex.Message });
            throw;
        }
    }

    private async Task<FeedbackResponse> ProcessFeedbackRequestAsync(FeedbackRequest request)
    {
        _logger.LogMethodEntry(nameof(ProcessFeedbackRequestAsync), request);

        try
        {
            // Simulate user interaction - in real implementation, this would show UI
            var response = new FeedbackResponse
            {
                Id = request.Id,
                Status = FeedbackStatus.Completed,
                UserResponse = await SimulateUserInteractionAsync(request),
                Rating = 4, // Default positive rating
                Comments = "Processed automatically for testing",
                Timestamp = DateTime.Now,
                ProcessingTime = TimeSpan.FromMilliseconds(100)
            };

            _logger.LogInfo($"Feedback request processed: {request.Type}", nameof(ProcessFeedbackRequestAsync));
            _logger.LogMethodExit(nameof(ProcessFeedbackRequestAsync), response);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Processing feedback request failed: {ex.Message}", ex, nameof(ProcessFeedbackRequestAsync));
            _logger.LogMethodExit(nameof(ProcessFeedbackRequestAsync), new { success = false, error = ex.Message });
            throw;
        }
    }

    private async Task<string> SimulateUserInteractionAsync(FeedbackRequest request)
    {
        // Simulate user interaction delay
        await Task.Delay(50);

        return request.Type switch
        {
            FeedbackType.Question => request.Options?.FirstOrDefault() ?? "Yes",
            FeedbackType.Validation => "Corrected input",
            FeedbackType.Feedback => "Good",
            FeedbackType.Progress => "Continue",
            FeedbackType.Confirmation => "Confirm",
            _ => "OK"
        };
    }

    private async Task<ValidationRuleResult> ApplyValidationRuleAsync(string input, ValidationRule rule)
    {
        await Task.Delay(1); // Simulate async validation

        var result = new ValidationRuleResult
        {
            IsValid = true,
            Messages = new List<string>(),
            SuggestedCorrections = new List<string>()
        };

        if (rule.RuleType == "Required" && string.IsNullOrWhiteSpace(input))
        {
            result.IsValid = false;
            result.Messages.Add($"{rule.FieldName} is required");
            result.SuggestedCorrections.Add($"Please provide a value for {rule.FieldName}");
        }
        else if (rule.RuleType == "MinLength" && input?.Length < rule.MinValue)
        {
            result.IsValid = false;
            result.Messages.Add($"{rule.FieldName} must be at least {rule.MinValue} characters");
            result.SuggestedCorrections.Add($"Please enter at least {rule.MinValue} characters");
        }
        else if (rule.RuleType == "MaxLength" && input?.Length > rule.MaxValue)
        {
            result.IsValid = false;
            result.Messages.Add($"{rule.FieldName} cannot exceed {rule.MaxValue} characters");
            result.SuggestedCorrections.Add($"Please limit to {rule.MaxValue} characters or less");
        }

        return result;
    }
}

/// <summary>
/// Feedback request model
/// </summary>
public class FeedbackRequest
{
    public string Id { get; set; } = string.Empty;
    public FeedbackType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; }
    public bool RequiresResponse { get; set; } = true;
    public TimeSpan? TimeoutDuration { get; set; }
    public int Priority { get; set; } = 0;
}

/// <summary>
/// Feedback response model
/// </summary>
public class FeedbackResponse
{
    public string Id { get; set; } = string.Empty;
    public FeedbackStatus Status { get; set; }
    public string? UserResponse { get; set; }
    public int Rating { get; set; } = 0;
    public string? Comments { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string? UserAction { get; set; }
    public Dictionary<string, object>? ModifiedParameters { get; set; }
}

/// <summary>
/// User feedback model
/// </summary>
public class UserFeedback
{
    public string Context { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public int Rating { get; set; } = 0;
    public string? Comments { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsPositive { get; set; } = true;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Validation request model
/// </summary>
public class ValidationRequest
{
    public List<ValidationRule>? ValidationRules { get; set; }
    public bool InteractiveValidation { get; set; } = true;
    public bool StrictMode { get; set; } = false;
    public string? Context { get; set; }
}

/// <summary>
/// Validation response model
/// </summary>
public class ValidationResponse
{
    public bool IsValid { get; set; } = true;
    public List<string> Messages { get; set; } = new();
    public List<string> SuggestedCorrections { get; set; } = new();
    public string? ProcessedInput { get; set; }
    public Dictionary<string, object>? ValidationData { get; set; }
}

/// <summary>
/// Validation rule model
/// </summary>
public class ValidationRule
{
    public string RuleType { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public int MinValue { get; set; } = 0;
    public int MaxValue { get; set; } = int.MaxValue;
    public string? Pattern { get; set; }
    public List<string>? AllowedValues { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Validation rule result model
/// </summary>
public class ValidationRuleResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Messages { get; set; } = new();
    public List<string> SuggestedCorrections { get; set; } = new();
}

/// <summary>
/// Progress information model
/// </summary>
public class ProgressInfo
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CurrentStep { get; set; } = 0;
    public int TotalSteps { get; set; } = 0;
    public double PercentComplete { get; set; } = 0;
    public string? CurrentOperation { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; } = TimeSpan.Zero;
    public bool CanCancel { get; set; } = true;
    public bool CanModify { get; set; } = false;
}

/// <summary>
/// Progress response model
/// </summary>
public class ProgressResponse
{
    public bool ShouldContinue { get; set; } = true;
    public string? UserAction { get; set; }
    public Dictionary<string, object>? ModifiedParameters { get; set; }
    public string? Feedback { get; set; }
}

/// <summary>
/// Feedback type enumeration
/// </summary>
public enum FeedbackType
{
    Question,
    Feedback,
    Validation,
    Progress,
    Confirmation,
    Warning,
    Error,
    Information
}

/// <summary>
/// Feedback status enumeration
/// </summary>
public enum FeedbackStatus
{
    Pending,
    Processing,
    Completed,
    Cancelled,
    TimedOut,
    Failed
}