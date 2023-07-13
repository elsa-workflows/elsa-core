using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Activities.Flowchart.Serialization;

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
        var targetId = targetElement.TryGetProperty("activity", out var targetIdValue) ? targetIdValue.GetString() : default;
        var sourcePort = sourceElement.GetProperty("port").GetString()!;
        var targetPort = targetElement.TryGetProperty("port", out var targetPortValue) ? targetPortValue.GetString() : default;

        var sourceActivity = _activities.TryGetValue(sourceId, out var s) ? s : default!;
        var targetActivity = targetId != null ? _activities.TryGetValue(targetId, out var t) ? t : default! : default!;
        var source = new Endpoint(sourceActivity, sourcePort);
        var target = new Endpoint(targetActivity, targetPort);
        return new Connection(source, target);
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
            }
        };

        JsonSerializer.Serialize(writer, model, options);
    }
}