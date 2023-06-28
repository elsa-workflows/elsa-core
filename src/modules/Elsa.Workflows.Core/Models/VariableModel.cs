using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents a shape of a variable that can be serialized.
/// </summary>
public class VariableModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VariableModel"/> class.
    /// </summary>
    [JsonConstructor]
    public VariableModel()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableModel"/> class.
    /// </summary>
    public VariableModel(string id, string name, string typeName, string? value, string? storageDriverTypeName)
    {
        Id = id;
        Name = name;
        TypeName = typeName;
        Value = value;
        StorageDriverTypeName = storageDriverTypeName;
    }

    /// <summary>
    /// Gets or sets the ID of the variable.
    /// </summary>
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the name of the variable.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the type name of the variable.
    /// </summary>
    public string TypeName { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the value of the variable.
    /// </summary>
    public string? Value { get; set; }
    
    /// <summary>
    /// Gets or sets the storage driver type name of the variable.
    /// </summary>
    public string? StorageDriverTypeName { get; set; }
}