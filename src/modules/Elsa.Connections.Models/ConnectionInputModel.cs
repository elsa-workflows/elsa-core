using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace Elsa.Connections.Models;

public class ConnectionInputModel
{
    /// <summary>
    /// The Name of the Connection
    /// </summary>
    [Required] public string Name { get; set; } = null!;

    /// <summary>
    /// The Description of the Connection
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// The Configuration of the Connection in a Json Format
    /// </summary>
    public JsonObject ConnectionConfiguration { get; set; } = new();

    /// <summary>
    /// The Connection Type Name
    /// </summary>
    [Required] public string ConnectionType { get; set; } = null!;
}