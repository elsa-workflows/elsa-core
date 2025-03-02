namespace Elsa.Connections.Attributes;

public class ConnectionTypeAttribute : Attribute
{
    public ConnectionTypeAttribute(Type type)
    {
        Type = type;
    }
    /// <summary>
    /// The TypeName of the connection
    /// </summary>
    public Type Type { get; set; }
}
