namespace Elsa.Connections.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ConnectionActivityAttribute(string type) : Attribute
{
    /// <summary>
    /// The TypeName of the connection
    /// </summary>
    public string Type { get; set; } = type;
}
