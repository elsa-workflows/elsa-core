namespace Elsa.Resilience;

public class ResilienceSourceNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}