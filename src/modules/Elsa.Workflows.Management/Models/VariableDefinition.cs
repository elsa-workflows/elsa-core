namespace Elsa.Workflows.Management.Models;

public record VariableDefinition(string Name, string TypeName, string? Value, string? StorageDriverId);