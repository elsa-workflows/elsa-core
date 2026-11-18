namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents a validation error.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="ActivityId">The Id of the activity that caused the error, if any.</param>
public record WorkflowValidationError(string Message, string? ActivityId = null)
{
    /// <summary>A stable identifier for clients that recognize the diagnostic.</summary>
    public string? Code { get; init; }

    /// <summary>Whether the diagnostic is advisory and permits publication.</summary>
    public bool IsWarning { get; init; }
}
