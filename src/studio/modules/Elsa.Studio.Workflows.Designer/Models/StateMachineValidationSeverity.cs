namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Represents the severity of a StateMachine validation issue.
/// </summary>
public enum StateMachineValidationSeverity
{
    /// <summary>
    /// The issue can be saved but should be shown to the user.
    /// </summary>
    Warning,

    /// <summary>
    /// The issue should block save until resolved.
    /// </summary>
    Error
}
