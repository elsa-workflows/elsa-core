namespace Elsa.Resilience;

[AttributeUsage(AttributeTargets.Class)]
public class ResilienceCategoryAttribute(string category) : Attribute
{
    public string Category { get; } = category;
}