using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.Logging.Models;

public sealed class SinkEnvelope
{
    public string Type { get; set; }
    public string Name { get; set; }
    public JsonElement? Options { get; set; }
    public JsonObject? Payload { get; set; }
}