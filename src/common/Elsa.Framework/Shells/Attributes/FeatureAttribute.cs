namespace Elsa.Framework.Shells;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class FeatureAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}