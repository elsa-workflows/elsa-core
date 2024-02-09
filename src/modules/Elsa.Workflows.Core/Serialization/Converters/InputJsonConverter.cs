using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Input"/> objects.
/// </summary>
public class InputJsonConverter<T> : JsonConverter<Input<T>>
{
    private readonly IExpressionDescriptorRegistry _expressionDescriptorRegistry;

    /// <inheritdoc />
    public InputJsonConverter(IExpressionDescriptorRegistry expressionDescriptorRegistry)
    {
        _expressionDescriptorRegistry = expressionDescriptorRegistry;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(Input).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override Input<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            return default!;

        // If the value is an object, it represents an Input-wrapped property.
        if (doc.RootElement.ValueKind == JsonValueKind.Object)
        {
            if (!doc.RootElement.TryGetProperty("typeName", out _))
                return default!;

            var expressionElement = doc.RootElement.TryGetProperty("expression", out var expressionElementValue) ? expressionElementValue : default;
            var expressionTypeNameElement = expressionElement.ValueKind != JsonValueKind.Undefined ? expressionElement.TryGetProperty("type", out var expressionTypeNameElementValue) ? expressionTypeNameElementValue : default : default;
            var expressionTypeName = expressionTypeNameElement.ValueKind != JsonValueKind.Undefined ? expressionTypeNameElement.GetString() ?? "Literal" : "Literal";
            var expressionDescriptor = _expressionDescriptorRegistry.Find(expressionTypeName);
            var memoryBlockReference = expressionDescriptor?.MemoryBlockReferenceFactory();
            var memoryBlockReferenceType = memoryBlockReference?.GetType();
            var expressionValueElement = expressionElement.TryGetProperty("value", out var expressionElementValueValue) ? expressionElementValueValue : default;
            var expressionValue = expressionValueElement.ValueKind == JsonValueKind.String
                ? expressionValueElement.GetString()
                : expressionValueElement.ValueKind != JsonValueKind.Undefined && memoryBlockReferenceType != null
                    ? expressionValueElement.Deserialize(memoryBlockReferenceType, options)!
                    : default;
            var expression = new Expression(expressionTypeName, expressionValue);

            if (memoryBlockReference == null)
                return default!;

            return (Input<T>)Activator.CreateInstance(typeof(Input<T>), expression, memoryBlockReference)!;
        }

        var convertedValue = doc.RootElement.ToString().TryConvertTo<T>().Value;
        return (Input<T>)Activator.CreateInstance(typeof(Input<T>), new Literal(convertedValue))!;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Input<T> value, JsonSerializerOptions options)
    {
        var expression = value.Expression;
        var expressionType = expression?.Type;
        var expressionDescriptor = expressionType != null ? _expressionDescriptorRegistry.Find(expressionType) : default;
        
        if (expressionDescriptor == null)
            throw new JsonException($"Could not find an expression descriptor for expression type '{expressionType}'.");
        
        var targetType = value.Type;
        var expressionValue = expressionDescriptor.IsSerializable ? expression : null;

        var model = new
        {
            TypeName = targetType,
            Expression = expressionValue!
        };

        JsonSerializer.Serialize(writer, model, options);
    }
}