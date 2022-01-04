using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Contracts;
using Elsa.Management.Contracts;
using Elsa.Management.Models;

namespace Elsa.Management.Serialization.Converters;

/// <summary>
/// (De)serializes objects of type <see cref="IActivity"/>.
/// </summary>
public class ActivityJsonConverter : JsonConverter<IActivity>
{
    private readonly IActivityRegistry _activityRegistry;
    private readonly IServiceProvider _serviceProvider;

    public ActivityJsonConverter(IActivityRegistry activityRegistry, IServiceProvider serviceProvider)
    {
        _activityRegistry = activityRegistry;
        _serviceProvider = serviceProvider;
    }
        
    public override IActivity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        if (!doc.RootElement.TryGetProperty("nodeType", out var activityTypeNameElement))
            throw new JsonException("Failed to extract activity type property");

        var activityTypeName = activityTypeNameElement.GetString()!;
        var activityDescriptor = _activityRegistry.Find(activityTypeName);

        if (activityDescriptor == null)
            throw new Exception($"Activity of type {activityTypeName} not found in registry");

        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Add(new InputJsonConverterFactory(_serviceProvider));
            
        var context = new ActivityConstructorContext(doc.RootElement, newOptions);
        var activity = activityDescriptor.Constructor(context);

        return activity;
    }

    public override void Write(Utf8JsonWriter writer, IActivity value, JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Add(new InputJsonConverterFactory(_serviceProvider));
        JsonSerializer.Serialize(writer, value, value.GetType(), newOptions);
    }
}