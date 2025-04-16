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

        var sourceElement = doc.RootElement.GetProperty("source");
        var targetElement = doc.RootElement.GetProperty("target");
        var sourceId = sourceElement.GetProperty("activity").GetString()!;
        var targetId = targetElement.TryGetProperty("activity", out var targetIdValue) ? targetIdValue.GetString() : null;
        var sourcePort = sourceElement.TryGetProperty("port", out var sourcePortValue) ? sourcePortValue.GetString() : null;
        var targetPort = targetElement.TryGetProperty("port", out var targetPortValue) ? targetPortValue.GetString() : null;
        var sourceActivity = _activities.TryGetValue(sourceId, out var s) ? s : null!;
        var targetActivity = targetId != null ? _activities.TryGetValue(targetId, out var t) ? t : null! : null!;
        var source = new Endpoint(sourceActivity, sourcePort);
        var target = new Endpoint(targetActivity, targetPort);
        var verticesElement = doc.RootElement.TryGetProperty("vertices", out var verticesValue) ? verticesValue : default;
        var vertices = Array.Empty<Position>();

        if (verticesElement.ValueKind == JsonValueKind.Array) 
            vertices = verticesElement.Deserialize<Position[]>(options)!;
        
        return new(source, target)
        {
            Vertices = vertices
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Connection value, JsonSerializerOptions options)
    {
        if (value.Source.Activity == null! || value.Target.Activity == null!)
            return;
        
        var model = new
        {
            Source = new
            {
                Activity = value.Source.Activity.Id,
                Port = value.Source.Port
            },
            Target = new
            {
                Activity = value.Target.Activity.Id,
                Port = value.Target.Port
            },
            Vertices = value.Vertices
        };

        JsonSerializer.Serialize(writer, model, options);
    }
}