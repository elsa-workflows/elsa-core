using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Elsa.Common.Entities;

namespace Elsa.Connections.Persistence.Entities;


public class ConnectionDefinition : Entity
{
    /// <summary>
    /// The Name of the Connection
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// The Description of the Connection
    /// </summary>
    public string? Description { get; set; } = default;


    /// <summary>
    /// The Configuration of the Connection in a Json Format
    /// </summary>
    public JsonObject ConnectionConfiguration { get; set; }

    /// <summary>
    /// The Connection Type Name
    /// </summary>
    public string ConnectionType { get; set; }
}