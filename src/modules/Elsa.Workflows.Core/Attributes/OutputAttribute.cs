namespace Elsa.Workflows.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class OutputAttribute : Attribute
{
    /// <summary>
    /// The technical name of the activity property.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// The user-friendly name of the activity property.
    /// </summary>
    public string? DisplayName { get; set; }
        
    /// <summary>
    /// A brief description about this property for workflow tooling to use when displaying activity editors.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// A value indicating whether this property should be visible.
    /// </summary>
    public bool IsBrowsable { get; set; } = true;
}