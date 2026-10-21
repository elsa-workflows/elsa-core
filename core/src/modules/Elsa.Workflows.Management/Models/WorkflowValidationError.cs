namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents a validation error.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="ActivityId">The Id of the activity that caused the error, if any.</param>
public record WorkflowValidationError(string Message, string? ActivityId = null)
{
    /// <summary>
    /// Gets an optional feature-owned machine-readable diagnostic code.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets optional feature-owned diagnostic data containing stable identifiers.
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Initializes a feature-owned structured validation diagnostic while retaining the original constructor.
    /// </summary>
    public WorkflowValidationError(string message, string? activityId, string? code, object? data)
        : this(message, activityId)
    {
        Code = code;
        Data = data;
    }
}
