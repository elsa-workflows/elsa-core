using System.Text.Json;

namespace Elsa.Workflows.Models;

/// <summary>
/// Selects an output converter and provides its per-binding settings.
/// </summary>
public sealed record OutputConverterConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutputConverterConfiguration"/> class.
    /// </summary>
    public OutputConverterConfiguration(string id, JsonElement? settings = null)
    {
        Id = id ?? string.Empty;
        Settings = settings?.Clone();
    }

    /// <summary>
    /// The stable converter ID.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Optional immutable JSON settings.
    /// </summary>
    public JsonElement? Settings { get; }
}
