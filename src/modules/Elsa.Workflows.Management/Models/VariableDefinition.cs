namespace Elsa.Workflows.Management.Models;

public record VariableDefinition(string Name, string Type, string? Value, string? StorageDriverId);