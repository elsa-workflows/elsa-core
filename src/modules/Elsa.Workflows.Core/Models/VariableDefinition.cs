namespace Elsa.Workflows.Models;

/// <summary>
/// Stores information about a workflow variable.
/// </summary>
public record VariableDefinition(string Id, string Name, string TypeName, bool IsArray, string? Value, string? StorageDriverTypeName);