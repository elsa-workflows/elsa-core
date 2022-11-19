using System.Runtime.CompilerServices;

namespace Elsa.Workflows.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class PortAttribute : Attribute
{
    public PortAttribute([CallerMemberName] string? name = default)
    {
        Name = name;
        DisplayName = name;
    }
    
    public PortAttribute(string name, string displayName)
    {
        Name = name;
        DisplayName = name;
    }
    
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
}