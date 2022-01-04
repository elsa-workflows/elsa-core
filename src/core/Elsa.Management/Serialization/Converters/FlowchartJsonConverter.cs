using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Activities.Workflows;
using Elsa.Contracts;
using Elsa.Extensions;

namespace Elsa.Management.Serialization.Converters;

public class FlowchartJsonConverter : JsonConverter<Flowchart>
{
    private readonly IActivityWalker _activityWalker;

    public FlowchartJsonConverter(IActivityWalker activityWalker)
    {
        _activityWalker = activityWalker;
    }

    public override Flowchart? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        var connectionsElement = doc.RootElement.GetProperty("connections");
        var metadataElement = doc.RootElement.GetProperty("metadata");
        var activitiesElement = doc.RootElement.GetProperty("activities");
        var id = doc.RootElement.GetProperty("id").GetString()!;
        var startId = doc.RootElement.TryGetProperty("start", out var startElement) ? startElement.GetString() : default;
        var rootActivities = activitiesElement.Deserialize<IActivity[]>(options) ?? Array.Empty<IActivity>();
        var metadata = metadataElement.Deserialize<IDictionary<string, object>>(options) ?? new Dictionary<string, object>();
        var start = rootActivities.FirstOrDefault(x => x.Id == startId) ?? rootActivities.FirstOrDefault();
        var connectionSerializerOptions = new JsonSerializerOptions(options);
        var activities = WalkActivityTree(rootActivities).ToList();
        var activityDictionary = activities.ToDictionary(x => x.Id);
        connectionSerializerOptions.Converters.Add(new ConnectionJsonConverter(activityDictionary));

        var connections = connectionsElement.Deserialize<Connection[]>(connectionSerializerOptions) ?? Array.Empty<Connection>();

        return new Flowchart
        {
            Id = id,
            Metadata = metadata,
            Activities = rootActivities,
            Connections = connections,
            Start = start,
        };
    }

    public override void Write(Utf8JsonWriter writer, Flowchart value, JsonSerializerOptions options)
    {
        var activities = value.Activities;
        var connectionSerializerOptions = new JsonSerializerOptions(options);
        var activityDictionary = activities.ToDictionary(x => x.Id);
        connectionSerializerOptions.Converters.Add(new ConnectionJsonConverter(activityDictionary));

        var model = new
        {
            NodeType = value.NodeType,
            Id = value.Id,
            Metadata = value.Metadata,
            Start = value.Start?.Id,
            Activities = value.Activities,
            Connections = value.Connections
        };

        JsonSerializer.Serialize(writer, model, connectionSerializerOptions);
    }

    private IEnumerable<IActivity> WalkActivityTree(IEnumerable<IActivity> rootActivities) => rootActivities
        .Select(rootActivity => _activityWalker.Walk(rootActivity))
        .SelectMany(node => node.Flatten().Select(x => x.Activity));
}