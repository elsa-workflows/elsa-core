namespace Elsa.Connections.Attributes;

public class ConnectionPropertyAttribute(string ns, string displayName, string? description = null) : Attribute
{
    public string? Namespace { get; set; } = ns;
    public string? Description { get; set; } = description;
    public string? DisplayName { get; set; } = displayName;
}