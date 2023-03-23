using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Activities.Flowchart.Serialization;

public class FlowchartJsonConverter : JsonConverter<Activities.Flowchart>
{
    private const string AllActivitiesKey = "AllActivities";
    private const string AllConnectionsKey = "AllConnections";

    public override Activities.Flowchart Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        var connectionsElement = doc.RootElement.TryGetProperty("connections", out var connectionsEl) ? connectionsEl : default;
        var activitiesElement = doc.RootElement.TryGetProperty("activities", out var activitiesEl) ? activitiesEl : default;
        var id = doc.RootElement.GetProperty("id").GetString()!;
        var startId = doc.RootElement.TryGetProperty("start", out var startElement) ? startElement.GetString() : default;
        var activities = activitiesElement.ValueKind != JsonValueKind.Undefined ? activitiesElement.Deserialize<ICollection<IActivity>>(options) ?? new List<IActivity>() : new List<IActivity>();
        var metadataElement = doc.RootElement.TryGetProperty("metadata", out var metadataEl) ? metadataEl : default;
        var metadata = metadataElement.ValueKind != JsonValueKind.Undefined ? metadataElement.Deserialize<IDictionary<string, object>>(options) ?? new Dictionary<string, object>() : new Dictionary<string, object>();
        var start = activities.FirstOrDefault(x => x.Id == startId) ?? activities.FirstOrDefault();
        var connectionSerializerOptions = new JsonSerializerOptions(options);
        var activityDictionary = activities.ToDictionary(x => x.Id);

        connectionSerializerOptions.Converters.Add(new ConnectionJsonConverter(activityDictionary));
        var connections = connectionsElement.ValueKind != JsonValueKind.Undefined 
            ? connectionsElement.Deserialize<ICollection<Connection>>(connectionSerializerOptions)?.Where(x => x.Source != null! && x.Target != null!).ToList() ?? new List<Connection>() 
            : new List<Connection>();

        var flowChart = new Activities.Flowchart
        {
            Id = id,
            Metadata = metadata,
            Start = start,
            Activities = activities,
            Connections = connections,
            CustomProperties =
            {
                [AllActivitiesKey] = activities.ToList(),
                [AllConnectionsKey] = connections.ToList()
            }
        };

        return flowChart;
    }

    public override void Write(Utf8JsonWriter writer, Activities.Flowchart value, JsonSerializerOptions options)
    {
        var activities = value.Activities;
        var connectionSerializerOptions = new JsonSerializerOptions(options);
        var activityDictionary = activities.ToDictionary(x => x.Id);

        connectionSerializerOptions.Converters.Add(new ConnectionJsonConverter(activityDictionary));

        var allActivities = value.CustomProperties.TryGetValue(AllActivitiesKey, out var a) ? a : activities;
        var allConnections = value.CustomProperties.TryGetValue(AllConnectionsKey, out var c) ? c : value.Connections;
        var applicationProperties = new Dictionary<string, object>(value.CustomProperties);

        applicationProperties.Remove(AllActivitiesKey);
        applicationProperties.Remove(AllConnectionsKey);

        var model = new
        {
            Type = value.Type,
            Version = value.Version,
            value.Id,
            value.Metadata,
            ApplicationProperties = applicationProperties,
            Start = value.Start?.Id,
            Activities = allActivities,
            Connections = allConnections
        };

        JsonSerializer.Serialize(writer, model, connectionSerializerOptions);
    }
}