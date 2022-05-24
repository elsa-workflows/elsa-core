namespace Elsa.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class OutputAttribute : Attribute
{
    /// <summary>
    /// The technical name of the activity property.
    /// </summary>
    public string? Name { get; set; }
        
    /// <summary>
    /// A brief description about this property for workflow tooling to use when displaying activity editors.
    /// </summary>
    public string? Description { get; set; }
}