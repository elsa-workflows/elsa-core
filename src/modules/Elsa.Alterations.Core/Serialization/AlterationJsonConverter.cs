using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Options;
using Elsa.Workflows.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Alterations.Core.Serialization;

/// <summary>
/// Creates instances of <see cref="ActivityJsonConverter"/>.
/// </summary>
public class AlterationJsonConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc />
    public AlterationJsonConverterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Notice that this factory only creates converters when the type to convert is IActivity.
    // The ActivityJsonConverter will create concrete activity objects, which then uses regular serialization
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(IAlteration);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => ActivatorUtilities.CreateInstance<AlterationJsonConverter>(_serviceProvider);
}

/// <summary>
/// A custom JSON converter for <see cref="IAlteration"/>.
/// </summary>
public class AlterationJsonConverter: JsonConverter<IAlteration>
{
    private readonly IOptions<AlterationOptions> _options;

    /// <inheritdoc />
    public AlterationJsonConverter(IOptions<AlterationOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type of the object to be deserialized is not known at compile time.")]
    public override IAlteration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        
        if (!jsonDocument.RootElement.TryGetProperty("type", out var typeElement))
            throw new JsonException();

        var typeStr = typeElement.GetString();
        var alterationType = _options.Value.AlterationTypes.FirstOrDefault(t => t.Name == typeStr);

        if (alterationType is null)
            throw new JsonException($"Unknown type: {typeStr}.");

        var alteration = (IAlteration)JsonSerializer.Deserialize(jsonDocument.RootElement.GetRawText(), alterationType)!;

        if (alteration is null)
            throw new JsonException();

        return alteration;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type of the object to be deserialized is not known at compile time.")]
    public override void Write(Utf8JsonWriter writer, IAlteration value, JsonSerializerOptions options)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var type = value.GetType();
        var typeName = type.Name;

        writer.WriteStartObject();
        writer.WriteString("type", typeName);
        //writer.WritePropertyName("data");
    
        // Serialization of actual object data, "value" is casted to the actual implementation type
        JsonSerializer.Serialize(writer, value, type, options);

        writer.WriteEndObject();
    }
}