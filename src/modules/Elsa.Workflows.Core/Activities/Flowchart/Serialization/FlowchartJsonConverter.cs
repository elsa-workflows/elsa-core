using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Activities.Flowchart.Serialization;

/// <summary>
/// A JSON converter for <see cref="Activities.Flowchart"/>.
/// </summary>
public class FlowchartJsonConverter(IIdentityGenerator identityGenerator, IWellKnownTypeRegistry wellKnownTypeRegistry) : JsonConverter<Activities.Flowchart>
{
    private const string AllActivitiesKey = "allActivities";
    private const string AllConnectionsKey = "allConnections";
    private const string NotFoundConnectionsKey = "notFoundConnections";
    
    /// <inheritdoc />
    public override Activities.Flowchart Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        var id = doc.RootElement.TryGetProperty("id", out var idAttribute) ? idAttribute.GetString()! : identityGenerator.GenerateId();
        var nodeId = doc.RootElement.TryGetProperty("nodeId", out var nodeIdAttribute) ? nodeIdAttribute.GetString() : default;
        var name = doc.RootElement.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : default;
        var type = doc.RootElement.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : default;
        var version = doc.RootElement.TryGetProperty("version", out var versionElement) ? versionElement.GetInt32() : 1;

        var connectionsElement = doc.RootElement.TryGetProperty("connections", out var connectionsEl) ? connectionsEl : default;
        var activitiesElement = doc.RootElement.TryGetProperty("activities", out var activitiesEl) ? activitiesEl : default;
        var activities = activitiesElement.ValueKind != JsonValueKind.Undefined ? activitiesElement.Deserialize<ICollection<IActivity>>(options) ?? new List<IActivity>() : new List<IActivity>();
        var activityDictionary = activities.ToDictionary(x => x.Id);
        var connections = DeserializeConnections(connectionsElement, activityDictionary, options);
        var notFoundConnections = GetNotFoundConnections(doc.RootElement, activityDictionary, connections, options);
        var connectionsToRestore = FindConnectionsThatCanBeRestored(notFoundConnections, activities);
        var connectionComparer = new ConnectionComparer();
        var connectionsWithRestoredOnes = connections.Except(notFoundConnections, connectionComparer).Union(connectionsToRestore, connectionComparer).ToList();

        var variablesElement = doc.RootElement.TryGetProperty("variables", out var variablesEl) ? variablesEl : default;
        var variables = variablesElement.ValueKind != JsonValueKind.Undefined ? variablesElement.Deserialize<ICollection<Variable>>(options) ?? new List<Variable>() : new List<Variable>();

        JsonSerializerOptions polymorphicOptions = options.Clone();
        polymorphicOptions.Converters.Add(new PolymorphicDictionaryConverter(options, wellKnownTypeRegistry));

        var metadataElement = doc.RootElement.TryGetProperty("metadata", out var metadataEl) ? metadataEl : default;
        var metadata = metadataElement.ValueKind != JsonValueKind.Undefined ? metadataElement.Deserialize<IDictionary<string, object>>(polymorphicOptions) ?? new Dictionary<string, object>() : new Dictionary<string, object>();

        var customPropertiesElement = doc.RootElement.TryGetProperty("customProperties", out var customPropertiesEl) ? customPropertiesEl : default;
        var customProperties = customPropertiesEl.ValueKind != JsonValueKind.Undefined ? customPropertiesElement.Deserialize<IDictionary<string, object>>(polymorphicOptions) ?? new Dictionary<string, object>() : new Dictionary<string, object>();
        customProperties[AllActivitiesKey] = activities.ToList();
        customProperties[AllConnectionsKey] = connectionsWithRestoredOnes;
        customProperties[NotFoundConnectionsKey] = notFoundConnections.Except(connectionsToRestore, connectionComparer).ToList();

        var flowChart = new Activities.Flowchart
        {
            Id = id,
            NodeId = nodeId!,
            Name = name,
            Type = type!,
            Version = version,
            CustomProperties = customProperties,
            Metadata = metadata,
            Activities = activities,
            Variables = variables,
            Connections = connectionsWithRestoredOnes,
        };

        return flowChart;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Activities.Flowchart value, JsonSerializerOptions options)
    {
        var activities = value.Activities;
        var activityDictionary = activities.ToDictionary(x => x.Id);

        var customProperties = new Dictionary<string, object>(value.CustomProperties);
        var allActivities = customProperties.GetValueOrDefault(AllActivitiesKey, activities);
        var allConnections = (ICollection<Connection>)(customProperties.TryGetValue(AllConnectionsKey, out var c) ? c : value.Connections);

        customProperties.Remove(AllActivitiesKey);
        customProperties.Remove(AllConnectionsKey);

        var model = new
        {
            value.Id,
            value.NodeId,
            value.Name,
            value.Type,
            value.Version,
            CustomProperties = customProperties,
            value.Metadata,
            Activities = allActivities,
            value.Variables,
            Connections = allConnections,
        };

        var flowchartSerializerOptions = new JsonSerializerOptions(options);
        flowchartSerializerOptions.Converters.Add(new ConnectionJsonConverter(activityDictionary));
        flowchartSerializerOptions.Converters.Add(new PolymorphicDictionaryConverter(options, wellKnownTypeRegistry));

        JsonSerializer.Serialize(writer, model, flowchartSerializerOptions);
    }

    private static ICollection<Connection> GetNotFoundConnections(JsonElement rootElement, IDictionary<string, IActivity> activities, IEnumerable<Connection> connections, JsonSerializerOptions connectionSerializerOptions)
    {
        var customPropertiesElement = rootElement.TryGetProperty("customProperties", out var customPropertiesEl) ? customPropertiesEl : default;

        var notFoundConnectionsElement = customPropertiesElement.ValueKind != JsonValueKind.Undefined ? customPropertiesElement.TryGetProperty(NotFoundConnectionsKey, out var notFoundConnectionsEl) ? notFoundConnectionsEl : default : default;
        var notFoundConnections = notFoundConnectionsElement.ValueKind != JsonValueKind.Undefined ? DeserializeConnections(notFoundConnectionsElement, activities, connectionSerializerOptions) : new List<Connection>();

        // Add connections of NotFoundActivity to the list if they aren't already in it.
        var notFoundActivities = activities.Values.Where(x => x is NotFoundActivity).Cast<NotFoundActivity>().ToList();
        var notFoundActivityConnections = connections.Where(x => notFoundActivities.Contains(x.Source.Activity)).ToList();

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

            // ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            // Activity might be null in case of JSON missing information.
            var source = foundActivities.FirstOrDefault(x => x.Id == missingSource.Activity?.Id);
            var target = foundActivities.FirstOrDefault(x => x.Id == missingTarget.Activity?.Id);
            // ReSharper restore ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

            if (source == null || target == null) continue;

            var connection = new Connection(new Endpoint(source, missingSource.Port), new Endpoint(target, missingTarget.Port));
            connectionsThatCanBeRestored.Add(connection);
        }

        return connectionsThatCanBeRestored;
    }

    private static ICollection<Connection> DeserializeConnections(JsonElement connectionsElement, IDictionary<string, IActivity> activityDictionary, JsonSerializerOptions options)
    {
        if (connectionsElement.ValueKind == JsonValueKind.Undefined)
            return new List<Connection>();

        // To not break existing workflow definitions, we need to support the old connection format.
        var useOldConnectionConverter = connectionsElement.EnumerateArray().Any(x => x.TryGetProperty("sourcePort", out var sourcePort) && sourcePort.ValueKind == JsonValueKind.String);

        var connectionSerializerOptions = new JsonSerializerOptions(options);

        if (useOldConnectionConverter)
        {
            connectionSerializerOptions.Converters.Add(new ObsoleteConnectionJsonConverter(activityDictionary));

            var obsoleteConnections = connectionsElement.ValueKind != JsonValueKind.Undefined
                ? connectionsElement.Deserialize<ICollection<ObsoleteConnection>>(connectionSerializerOptions)?.Where(x => x.Source != null! && x.Target != null!).ToList() ?? new List<ObsoleteConnection>()
                : new List<ObsoleteConnection>();

            return obsoleteConnections.Select(x => new Connection(new Endpoint(x.Source, x.SourcePort), new Endpoint(x.Target, x.TargetPort))).ToList();
        }

        connectionSerializerOptions.Converters.Add(new ConnectionJsonConverter(activityDictionary));

        return connectionsElement.ValueKind != JsonValueKind.Undefined
            ? connectionsElement.Deserialize<ICollection<Connection>>(connectionSerializerOptions)?.Where(x => x.Source != null! && x.Target != null!).ToList() ?? new List<Connection>()
            : new List<Connection>();
    }
}