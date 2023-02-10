namespace Elsa.JavaScript.TypeDefinitions.Models;

/// <summary>
/// A property definition.
/// </summary>
public class PropertyDefinition
{
    /// <summary>
    /// The name of the property.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// The type name of the property.
    /// </summary>
    public string Type { get; set; } = default!;
    
    /// <summary>
    /// A value indicating whether the property is optional or not.
    /// </summary>
    public bool IsOptional { get; set; }
}