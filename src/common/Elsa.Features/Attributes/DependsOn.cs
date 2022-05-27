namespace Elsa.Features.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependsOn : Attribute
{
    public DependsOn(Type type)
    {
        Type = type;
    }
    
    /// <summary>
    /// The type of the configurator this configurator depends on.
    /// </summary>
    public Type Type { get; set; }
}