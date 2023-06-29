using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Converters;

/// <summary>
/// A JSON converter that serializes <see cref="Activity"/>.
/// </summary>
public class ActivityJsonConverter : JsonConverter<Activity>
{
    private readonly IActivityTypeService _activityTypeService;

    /// <inheritdoc />
    public ActivityJsonConverter(IActivityTypeService activityTypeService)
    {
        _activityTypeService = activityTypeService;
    }

    /// <inheritdoc />
    public override Activity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        var activityRoot = doc.RootElement;
        var activityTypeName = activityRoot.GetProperty("type").GetString()!;
        var activityType = _activityTypeService.ResolveType(activityTypeName);
        var newOptions = new JsonSerializerOptions(options);
        var activity = (Activity)JsonSerializer.Deserialize(activityRoot.GetRawText(), activityType, newOptions)!;
      
        return activity;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Activity value, JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        writer.WriteStartObject();
        
        foreach (var prop in value.Where(kvp => kvp.Value != null))
        {
            writer.WritePropertyName(prop.Key);
            JsonSerializer.Serialize(writer, prop.Value, prop.Value.GetType(), newOptions);
        }
        
        writer.WriteEndObject();
    }
}