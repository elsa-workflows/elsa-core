namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// Represents a workflow variable.
/// </summary>
public class Variable
{
    /// <summary>
    /// The ID of the variable.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// The name of the variable.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// The type name of the variable.
    /// </summary>
    public string TypeName { get; set; } = default!;
    
    /// <summary>
    /// Indicates whether the variable is an array.
    /// </summary>
    public bool IsArray { get; set; }
    
    /// <summary>
    /// The default value of the variable.
    /// </summary>
    public object? Value { get; set; }
    
    /// <summary>
    /// The storage driver type to use for persistence.
    /// </summary>
    public string? StorageDriverTypeName { get; set; }
    
    /// <summary>
    /// The type name of the variable, including the array indicator.
    /// </summary>
    public string GetTypeDisplayName() => IsArray ? $"{TypeName}[]" : TypeName;
}