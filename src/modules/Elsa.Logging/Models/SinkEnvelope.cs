using System.Text.Json;

namespace Elsa.Logging.Models;

public sealed class SinkEnvelope
{
    public string Type { get; set; }
    public string Name { get; set; }
    public JsonElement Options { get; set; }
}