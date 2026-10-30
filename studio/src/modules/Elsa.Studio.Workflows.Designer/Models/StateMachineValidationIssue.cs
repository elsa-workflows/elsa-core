namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Represents a validation issue in a StateMachine graph.
/// </summary>
public class StateMachineValidationIssue
{
    /// <summary>
    /// Gets or sets the issue severity.
    /// </summary>
    public StateMachineValidationSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets a stable issue code.
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// Gets or sets the user-facing issue message.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Gets or sets the issue target.
    /// </summary>
    public string? Target { get; set; }
}
