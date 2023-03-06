using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Serialization;

public class ConnectionJsonConverter : JsonConverter<Connection>
{
    private readonly IDictionary<string, IActivity> _activities;

    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Connection);

    public ConnectionJsonConverter(IDictionary<string, IActivity> activities)
    {
        _activities = activities;
    }

    public override Connection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        var sourceId = doc.RootElement.GetProperty("source").GetString()!;
        var targetId = doc.RootElement.GetProperty("target").GetString()!;
        var sourcePort = doc.RootElement.GetProperty("sourcePort").GetString()!;
        var targetPort = doc.RootElement.GetProperty("targetPort").GetString()!;

        var source = _activities.TryGetValue(sourceId, out var s) ? s : default!;
        var target = _activities.TryGetValue(targetId, out var t) ? t : default!;

        return new Connection(source, target, sourcePort, targetPort);
    }

    public override void Write(Utf8JsonWriter writer, Connection value, JsonSerializerOptions options)
    {
        var (activity, target, sourcePort, targetPort) = value;

        var model = new
        {
            Source = activity.Id,
            Target = target.Id,
            SourcePort = sourcePort,
            TargetPort = targetPort
        };

        JsonSerializer.Serialize(writer, model, options);
    }
}