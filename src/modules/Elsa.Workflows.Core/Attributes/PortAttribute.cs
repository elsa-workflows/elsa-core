namespace Elsa.Workflows.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class PortAttribute : Attribute
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
}