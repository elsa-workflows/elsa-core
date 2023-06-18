using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Activities.Flowchart.Serialization;

/// <summary>
/// Converts <see cref="ObsoleteConnection"/> to and from JSON.
/// </summary>
[Obsolete("Use ConnectionJsonConverter instead.")]
public class ObsoleteConnectionJsonConverter : JsonConverter<ObsoleteConnection>
{
    private readonly IDictionary<string, IActivity> _activities;

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(ObsoleteConnection);

    /// <inheritdoc />
    public ObsoleteConnectionJsonConverter(IDictionary<string, IActivity> activities)
    {
        _activities = activities;
    }

    /// <inheritdoc />
    public override ObsoleteConnection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        var sourceId = doc.RootElement.GetProperty("source").GetString()!;
        var targetId = doc.RootElement.GetProperty("target").GetString()!;
        var sourcePort = doc.RootElement.GetProperty("sourcePort").GetString()!;
        var targetPort = doc.RootElement.GetProperty("targetPort").GetString()!;

        var source = _activities.TryGetValue(sourceId, out var s) ? s : default!;
        var target = _activities.TryGetValue(targetId, out var t) ? t : default!;

        return new ObsoleteConnection(source, target, sourcePort, targetPort);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ObsoleteConnection value, JsonSerializerOptions options)
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