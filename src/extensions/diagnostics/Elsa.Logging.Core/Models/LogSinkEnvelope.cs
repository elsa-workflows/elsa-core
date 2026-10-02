using System.Text.Json;

namespace Elsa.Logging.Models;

/// <summary>
/// Represents an envelope for defining a log sink within the logging framework.
/// This class serves as a container for specifying the type, name, and options for configuring log sinks.
/// </summary>
public sealed class LogSinkEnvelope
{
    public string Type { get; set; } = null!;
    public string Name { get; set; } = null!;
    public JsonElement Options { get; set; }
}