using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Activities.Workflows;
using Elsa.Contracts;

namespace Elsa.Management.Serialization.Converters;

public class ConnectionJsonConverter : JsonConverter<Connection>
{
    private readonly IDictionary<string, IActivity> _activities;

    public ConnectionJsonConverter(IDictionary<string, IActivity> activities)
    {
        _activities = activities;
    }
    
    public override Connection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        var sourceId = doc.RootElement.GetProperty("source").GetString()!;
        var targetId = doc.RootElement.GetProperty("target").GetString()!;
        var sourcePort = doc.RootElement.GetProperty("sourcePort").GetString()!;
        var targetPort = doc.RootElement.GetProperty("targetPort").GetString()!;

        var source = _activities[sourceId];
        var target = _activities[targetId];

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