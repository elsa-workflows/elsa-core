namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents an activity input name and value.
/// </summary>
public record WorkflowInput(string Name, object? Value);