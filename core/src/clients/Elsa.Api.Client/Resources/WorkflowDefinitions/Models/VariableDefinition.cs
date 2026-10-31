namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// Stores information about a workflow variable.
/// </summary>
public class VariableDefinition
{
    /// <summary>
    /// Gets or sets the ID of the variable.
    /// </summary>
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the name of the variable.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the type of the variable.
    /// </summary>
    public string TypeName { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets whether the variable is an array.
    /// </summary>
    public bool IsArray { get; set; }
    
    /// <summary>
    /// Gets or sets the default value of the variable.
    /// </summary>
    public string? Value { get; set; }
    
    /// <summary>
    /// Gets or sets the storage driver type name of the variable.
    /// </summary>
    public string? StorageDriverTypeName { get; set; }
}