using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Contracts;
using Elsa.Management.Contracts;
using Elsa.Management.Models;

namespace Elsa.Management.Serialization.Converters;

/// <summary>
/// (De)serializes objects of type <see cref="ITrigger"/>.
/// </summary>
public class TriggerJsonConverter : JsonConverter<ITrigger>
{
    private readonly ITriggerRegistry _triggerRegistry;
    private readonly IServiceProvider _serviceProvider;

    public TriggerJsonConverter(ITriggerRegistry triggerRegistry, IServiceProvider serviceProvider)
    {
        _triggerRegistry = triggerRegistry;
        _serviceProvider = serviceProvider;
    }
        
    public override ITrigger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        if (!doc.RootElement.TryGetProperty("nodeType", out var triggerTypeNameElement))
            throw new JsonException("Failed to extract trigger type property");

        var triggerType = triggerTypeNameElement.GetString()!;
        var triggerDescriptor = _triggerRegistry.Find(triggerType);

        if (triggerDescriptor == null)
            throw new Exception($"Trigger of type {triggerType} not found in registry");

        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Add(new InputJsonConverterFactory(_serviceProvider));
            
        var context = new TriggerConstructorContext(doc.RootElement, newOptions);
        var activity = triggerDescriptor.Constructor(context);

        return activity;
    }

    public override void Write(Utf8JsonWriter writer, ITrigger value, JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Add(new InputJsonConverterFactory(_serviceProvider));
        JsonSerializer.Serialize(writer, value, value.GetType(), newOptions);
    }
}