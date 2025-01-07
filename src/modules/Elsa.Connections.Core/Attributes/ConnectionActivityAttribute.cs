namespace Elsa.Connections.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ConnectionActivityAttribute : Attribute
{

    public ConnectionActivityAttribute(string type)
    {
        Type = type;
    }
    /// <summary>
    /// The TypeName of the connection
    /// </summary>
    public string Type { get; set; }
}
