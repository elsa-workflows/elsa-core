namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Stores information about a workflow variable.
/// </summary>
public record VariableDefinition(string Name, string TypeName, string? Value, string? StorageDriverTypeName);