using System.Text.Json;

namespace Elsa.Logging.Models;

public sealed class LogSinkEnvelope
{
    public string Type { get; set; } = null!;
    public string Name { get; set; } = null!;
    public JsonElement Options { get; set; }
}