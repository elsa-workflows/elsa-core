namespace Elsa.Resilience;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class ResilienceCategoryAttribute(string category) : Attribute
{
    public string Category { get; } = category;
}