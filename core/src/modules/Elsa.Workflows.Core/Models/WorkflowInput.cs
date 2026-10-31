namespace Elsa.Workflows.Models;

/// <summary>
/// Represents a workflow input name and value.
/// </summary>
public record WorkflowInput(string Name, object? Value);