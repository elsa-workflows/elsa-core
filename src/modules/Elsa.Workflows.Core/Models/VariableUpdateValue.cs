namespace Elsa.Workflows;

/// <summary>
/// Represents a variable to update and its new value.
/// </summary>
/// <param name="Id">The ID of the variable to update.</param>
/// <param name="Value">The new value of the variable.</param>
public record VariableUpdateValue(string Id, object? Value);