namespace Elsa.Workflows.Api.Models;

public record VariableDefinition(string Name, string Type, string? Value, string? StorageDriverId);