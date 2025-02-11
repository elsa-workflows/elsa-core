using System.Text.Json.Serialization;

namespace Elsa.Connections.Models;

public class ConnectionProperties<T> where T : class, new()
{
    /// <summary>
    /// Creates a new instance of the <see cref="ConnectionProperties"/> class.
    /// </summary>
    [JsonConstructor]
    public ConnectionProperties()
    {
    }

    public string? ConnectionName { get; set; }

    [JsonIgnore]
    public T Properties { get; set; } = new T();
}

