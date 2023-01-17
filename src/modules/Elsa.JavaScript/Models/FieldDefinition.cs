namespace Elsa.JavaScript.Models;

/// <summary>
/// A field definition.
/// </summary>
public class FieldDefinition
{
    /// <summary>
    /// The name of the field.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// The type name of the field.
    /// </summary>
    public string Type { get; set; } = default!;
    
    /// <summary>
    /// A value indicating whether the field is optional or not.
    /// </summary>
    public bool IsOptional { get; set; }
}