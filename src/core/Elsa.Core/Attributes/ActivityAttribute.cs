namespace Elsa.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ActivityAttribute : Attribute
{
    public ActivityAttribute(string? typeName = default)
    {
        TypeName = typeName;
    }
        
    public string? TypeName { get; }
}