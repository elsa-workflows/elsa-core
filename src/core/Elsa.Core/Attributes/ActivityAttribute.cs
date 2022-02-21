namespace Elsa.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ActivityAttribute : Attribute
{
    public ActivityAttribute(string? typeName = default, string? description = default, string? category = default)
    {
        TypeName = typeName;
        Description = description;
        Category = category;
    }
        
    public string? TypeName { get; }
    public string? Description { get; }
    public string? Category { get; }
}