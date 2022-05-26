namespace Elsa.ServiceConfiguration.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependencyAttribute : Attribute
{
    public DependencyAttribute(Type type)
    {
        Type = type;
    }
    
    /// <summary>
    /// The type of the configurator this configurator depends on.
    /// </summary>
    public Type Type { get; set; }
}