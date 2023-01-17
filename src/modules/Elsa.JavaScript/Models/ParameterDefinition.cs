namespace Elsa.JavaScript.Models;

/// <summary>
/// A method parameter definition.
/// </summary>
public class ParameterDefinition
{
    /// <summary>
    /// The name of the field.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// The type name of the field.
    /// </summary>
    public Type Type { get; set; } = default!;
    
    /// <summary>
    /// A value indicating whether the field is optional or not.
    /// </summary>
    public bool IsOptional { get; set; }
}