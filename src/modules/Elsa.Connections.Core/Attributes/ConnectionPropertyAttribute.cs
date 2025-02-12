namespace Elsa.Connections.Attributes;

public class ConnectionPropertyAttribute : Attribute
{
    public ConnectionPropertyAttribute(string @namespace, string displayName, string? description= default)
    {
        Namespace = @namespace;
        DisplayName = displayName;
        Description = description;

    }
    public string? Namespace { get; set; }
    public string? Description { get; set; }
    public string? DisplayName { get; set; }
}