using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace Elsa.Connections.Models;

public class ConnectionConfigurationMetadataModel
{
    public string Id { get; set; }
    [Required]
    public string? Name { get; set; }
    public string Description { get; set; }

    public JsonObject ConnectionConfiguration { get; set; }

    [Required]
    public string ConnectionType { get; set; }
}