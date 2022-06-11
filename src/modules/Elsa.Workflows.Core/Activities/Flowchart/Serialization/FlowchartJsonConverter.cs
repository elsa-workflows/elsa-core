using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Activities.Flowchart.Services;
using Elsa.Workflows.Core.Activities.Flowchart.Transposition.Services;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Serialization;

public class FlowchartJsonConverter : JsonConverter<Activities.Flowchart>
{
    private readonly IActivityWalker _activityWalker;
    private readonly IActivityNodeDescriber _activityNodeDescriber;

    public FlowchartJsonConverter(IActivityWalker activityWalker, IActivityNodeDescriber activityNodeDescriber)
    {
        _activityWalker = activityWalker;
        _activityNodeDescriber = activityNodeDescriber;
    }

    public override Activities.Flowchart? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        var connectionsElement = doc.RootElement.GetProperty("connections");
        var metadataElement = doc.RootElement.GetProperty("metadata");
        var activitiesElement = doc.RootElement.GetProperty("activities");
        var id = doc.RootElement.GetProperty("id").GetString()!;
        var startId = doc.RootElement.TryGetProperty("start", out var startElement) ? startElement.GetString() : default;
        var activities = activitiesElement.Deserialize<ICollection<IActivity>>(options) ?? new List<IActivity>();
        var activityTypes = activities.Select(x => x.GetType()).Distinct().ToList();
        var activityNodeDescriptors = activityTypes.Select(_activityNodeDescriber.DescribeActivity).ToDictionary(x => x.ActivityRuntimeType);
        var metadata = metadataElement.Deserialize<IDictionary<string, object>>(options) ?? new Dictionary<string, object>();
        var start = activities.FirstOrDefault(x => x.Id == startId) ?? activities.FirstOrDefault();
        var connectionSerializerOptions = new JsonSerializerOptions(options);
        var activityDictionary = activities.ToDictionary(x => x.Id);
        connectionSerializerOptions.Converters.Add(new ConnectionJsonConverter(activityDictionary));

        var connections = connectionsElement.Deserialize<ICollection<Connection>>(connectionSerializerOptions) ?? new List<Connection>();
        var transposedStart = Transpose(start, activityDictionary, connections, activityNodeDescriptors);

        return new Activities.Flowchart
        {
            Id = id,
            Metadata = metadata,
            Activities = activities,
            Connections = connections,
            Start = start,
        };
    }

    public override void Write(Utf8JsonWriter writer, Activities.Flowchart value, JsonSerializerOptions options)
    {
        var activities = value.Activities;
        var connectionSerializerOptions = new JsonSerializerOptions(options);
        var activityDictionary = activities.ToDictionary(x => x.Id);
        connectionSerializerOptions.Converters.Add(new ConnectionJsonConverter(activityDictionary));

        var model = new
        {
            value.TypeName,
            value.Id,
            value.Metadata,
            value.ApplicationProperties,
            Start = value.Start?.Id,
            value.Activities,
            value.Connections
        };

        JsonSerializer.Serialize(writer, model, connectionSerializerOptions);
    }

    /// <summary>
    /// Starting from the root, find each outbound connection and transpose each connection by constructing a sub-flow in case the source port is an actual outbound property on the source activity.
    /// For example, the <see cref="ForEach"/> activity has a <see cref="ForEach.Body"/> outbound activity property.
    /// We follow the outbound connection, take the target activity, and assign it to the Body property.
    /// </summary>
    private IActivity? Transpose(IActivity? start, IDictionary<string, IActivity> activities, ICollection<Connection> connections, IDictionary<Type, ActivityNodeDescriptor> descriptors)
    {
        if (start == null)
            return null;

        var outboundConnections = connections.Where(x => x.Source == start).ToList();
        

        foreach (var connection in outboundConnections)
        {
            var transposeContext = new TransposeContext(connection,)
        }
    }
}