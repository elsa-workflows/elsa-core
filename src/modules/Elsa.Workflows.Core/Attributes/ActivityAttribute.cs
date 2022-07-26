namespace Elsa.Workflows.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ActivityAttribute : Attribute
{
    public ActivityAttribute(string @namespace, string? category, string? description = default)
    {
        Namespace = @namespace;
        Description = description;
        Category = category;
    }

    public ActivityAttribute(string @namespace, string? description = default)
    {
        Namespace = @namespace;
        Description = description;
        Category = @namespace;
    }

    public ActivityAttribute(string @namespace, string? typeName, string? description = default, string? category = default)
    {
        Namespace = @namespace;
        TypeName = typeName;
        Description = description;
        Category = category;
    }

    public string? Namespace { get; set;}
    public string? TypeName { get; set;}
    public string? Description { get; set;}
    public string? DisplayName { get; set; }
    public string? Category { get; set;}
}