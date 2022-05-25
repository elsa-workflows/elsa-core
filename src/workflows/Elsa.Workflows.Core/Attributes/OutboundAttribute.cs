namespace Elsa.Workflows.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class OutboundAttribute : Attribute
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
}