using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Activities.Flowchart.Serialization;

/// <summary>
/// A JSON converter for <see cref="Activities.Flowchart"/>.
/// </summary>
public class FlowchartJsonConverter : JsonConverter<Activities.Flowchart>
{
    private const string AllActivitiesKey = "AllActivities";
    private const string AllConnectionsKey = "AllConnections";
    private const string NotFoundConnectionsKey = "NotFoundConnectionsKey";

    /// <inheritdoc />
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
        
        var notFoundConnections = GetNotFoundConnections(doc.RootElement, connectionSerializerOptions, activities, connections);
        var connectionsToRestore = FindConnectionsThatCanBeRestored(notFoundConnections, activities);
        var connectionsWithRestoredOnes = connections.Except(notFoundConnections).Union(connectionsToRestore).ToList();

        var flowChart = new Activities.Flowchart
        {
            Id = id,
            Metadata = metadata,
            Start = start,
            Activities = activities,
            Connections = connectionsWithRestoredOnes,
            CustomProperties =
            {
                [AllActivitiesKey] = activities.ToList(),
                [AllConnectionsKey] = connectionsWithRestoredOnes,
                [NotFoundConnectionsKey] = notFoundConnections.Except(connectionsToRestore)
            }
        };

        return flowChart;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Activities.Flowchart value, JsonSerializerOptions options)
    {
        var activities = value.Activities;
        var connectionSerializerOptions = new JsonSerializerOptions(options);
        var activityDictionary = activities.ToDictionary(x => x.Id);

        connectionSerializerOptions.Converters.Add(new ConnectionJsonConverter(activityDictionary));

        var allActivities = value.CustomProperties.TryGetValue(AllActivitiesKey, out var a) ? a : activities;
        var allConnections = (ICollection<Connection>)(value.CustomProperties.TryGetValue(AllConnectionsKey, out var c) ? c : value.Connections);
        var applicationProperties = new Dictionary<string, object>(value.CustomProperties);

        applicationProperties.Remove(AllActivitiesKey);
        applicationProperties.Remove(AllConnectionsKey);

        var model = new
        {
            value.Type,
            value.Version,
            value.Id,
            value.Metadata,
            ApplicationProperties = applicationProperties,
            Start = value.Start?.Id,
            Activities = allActivities,
            Connections = allConnections
        };

        JsonSerializer.Serialize(writer, model, connectionSerializerOptions);
    }
    
    private static List<Connection> GetNotFoundConnections(JsonElement rootElement, JsonSerializerOptions connectionSerializerOptions, IEnumerable<IActivity> activities, IEnumerable<Connection> connections)
    {
        var applicationPropertiesElement = rootElement.TryGetProperty("applicationProperties", out var applicationPropertiesEl) ? applicationPropertiesEl : default;
        var notFoundConnectionsElement = applicationPropertiesElement.ValueKind != JsonValueKind.Undefined ? applicationPropertiesElement.TryGetProperty(NotFoundConnectionsKey, out var notFoundConnectionsEl) ? notFoundConnectionsEl : default : default;
        
        var notFoundConnections = notFoundConnectionsElement.ValueKind != JsonValueKind.Undefined
            ? notFoundConnectionsElement.Deserialize<ICollection<Connection>>(connectionSerializerOptions)
                ?.Where(x => x.Source != null! && x.Target != null!).ToList() ?? new List<Connection>()
            : new List<Connection>();

        // Add connections of NotFoundActivity to the list if they aren't already in it.
        var notFoundActivities = activities.Where(x => x is NotFoundActivity).Cast<NotFoundActivity>().ToList();
        var notFoundActivityConnections = connections.Where(x => notFoundActivities.Contains(x.Source)).ToList();
        
        foreach (var notFoundConnection in notFoundActivityConnections)
        {
            if (notFoundConnections.All(x => x.Source != notFoundConnection.Source || x.Target != notFoundConnection.Target))
                notFoundConnections.Add(notFoundConnection);
        }

        return notFoundConnections;
    }

    private static List<Connection> FindConnectionsThatCanBeRestored(IEnumerable<Connection> notFoundConnections, IEnumerable<IActivity> activities)
    {
        var connectionsThatCanBeRestored = new List<Connection>();
        var foundActivities = activities.Where(x => x is not NotFoundActivity).ToList();
        
        foreach (var notFoundConnection in notFoundConnections.ToList())
        {
            var missingSource = notFoundConnection.Source;
            var missingTarget = notFoundConnection.Target;
            var source = foundActivities.FirstOrDefault(x => x.Id == missingSource.Id);
            var target = foundActivities.FirstOrDefault(x => x.Id == missingTarget.Id);

            if (source == null || target == null) continue;
            
            var connection = notFoundConnection with {Source = source, Target = target};
            connectionsThatCanBeRestored.Add(connection);
        }

        return connectionsThatCanBeRestored;
    }
}