using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Serialization;

/// <summary>
/// Converts <see cref="Connection"/> to and from JSON.
/// </summary>
public class ConnectionJsonConverter : JsonConverter<Connection>
{
    private readonly IDictionary<string, IActivity> _activities;

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Connection);

    /// <inheritdoc />
    public ConnectionJsonConverter(IDictionary<string, IActivity> activities)
    {
        _activities = activities;
    }

    /// <inheritdoc />
    public override Connection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        var root = doc.RootElement;

        // case‐insensitive get
        JsonElement Get(string name)
        {
            if (root.TryGetProperty(name, out var e))
                return e;
            var alt = char.ToUpperInvariant(name[0]) + name.Substring(1);
            if (root.TryGetProperty(alt, out e))
                return e;
            throw new JsonException($"Missing property '{name}' or '{alt}'");
        }

        var sourceElement = Get("source");
        var targetElement = Get("target");

        // now inside sourceElement and targetElement, their children
        // are again PascalCased (“Activity”, “Port”), so do the same thing:

        string GetId(JsonElement container, string propName)
        {
            if (container.TryGetProperty(propName, out var p))
                return p.GetString()!;
            var alt = char.ToUpperInvariant(propName[0]) + propName.Substring(1);
            return container.GetProperty(alt).GetString()!;
        }

        string? GetPort(JsonElement container, string propName)
        {
            if (container.TryGetProperty(propName, out var p))
                return p.GetString();
            var alt = char.ToUpperInvariant(propName[0]) + propName.Substring(1);
            return container.TryGetProperty(alt, out p) ? p.GetString() : null;
        }

        var sourceId = GetId(sourceElement, "activity");
        var targetId = GetPort(targetElement, "activity"); // note: this could be null
        var sourcePort = GetPort(sourceElement, "port");
        var targetPort = GetPort(targetElement, "port");

        var sourceAct = _activities.TryGetValue(sourceId, out var s) ? s : throw new JsonException($"Unknown activity ID '{sourceId}'");
        var targetAct =
            targetId != null
                ? _activities.TryGetValue(targetId, out var t)
                    ? t
                    : throw new JsonException($"Unknown activity ID '{targetId}'")
                : null;

        var source = new Endpoint(sourceAct, sourcePort);
        var target = new Endpoint(targetAct!, targetPort);

        // vertices is already correct:
        var vertices = Array.Empty<Position>();
        if (doc.RootElement.TryGetProperty("vertices", out var vertsEl) && vertsEl.ValueKind == JsonValueKind.Array)
            vertices = vertsEl.Deserialize<Position[]>(options)!;

        return new Connection(source, target) { Vertices = vertices };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Connection value, JsonSerializerOptions options)
    {
        if (value.Source.Activity == null! || value.Target.Activity == null!)
            return;

        var model = new
        {
            Source = new { Activity = value.Source.Activity.Id, Port = value.Source.Port },
            Target = new { Activity = value.Target.Activity.Id, Port = value.Target.Port },
            Vertices = value.Vertices,
        };

        JsonSerializer.Serialize(writer, model, options);
    }
}
