namespace Elsa.Connections.Models;

public class ConnectionOptions
{
    /// <summary>
    /// A collection of connection types that are available to the system.
    /// </summary>
    public HashSet<Type> ConnectionTypes { get; set; } = new();
}