namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents a description of a .NET type that can be used as a workflow variable.
/// </summary>
public record VariableDescriptor(Type Type, string Category, string? Description);