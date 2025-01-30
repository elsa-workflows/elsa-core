using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Elsa.Connections.Models;

public class ConnectionConfigurationModel
{
    public string Id { get; set; }
    [Required]
    public string? Name { get; set; }
    public string Description { get; set; }

    public JsonObject ConnectionConfiguration { get; set; }

    [Required]
    public string ConnectionType { get; set; }
}
