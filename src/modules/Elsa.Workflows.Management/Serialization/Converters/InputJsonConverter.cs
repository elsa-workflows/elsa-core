using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Management.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Input"/> objects.
/// </summary>
public class InputJsonConverter<T> : JsonConverter<Input<T>>
{
    private readonly IExpressionSyntaxRegistry _expressionSyntaxRegistry;

    /// <inheritdoc />
    public InputJsonConverter(IExpressionSyntaxRegistry expressionSyntaxRegistry)
    {
        _expressionSyntaxRegistry = expressionSyntaxRegistry;
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
            var expressionTypeName = expressionTypeNameElement.ValueKind != JsonValueKind.Undefined ? expressionTypeNameElement.GetString() ?? "Literal" : default;
            var expressionSyntaxDescriptor = expressionTypeName != null ? _expressionSyntaxRegistry.Find(expressionTypeName) : default;

            doc.RootElement.TryGetProperty("memoryReference", out var memoryReferenceElement);

            var memoryReferenceId = memoryReferenceElement.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? default
                : memoryReferenceElement.TryGetProperty("id", out var memoryReferenceIdElement)
                    ? memoryReferenceIdElement.GetString()
                    : default;

            var context = new ExpressionConstructorContext(expressionElement, options);
            var expression = expressionSyntaxDescriptor?.CreateExpression(context);
            var memoryBlockReference = expression != null ? expressionSyntaxDescriptor?.CreateBlockReference(new BlockReferenceConstructorContext(expression)) : memoryReferenceId != null ? new ReadReference(memoryReferenceId) : default;

            if (memoryBlockReference == null)
                return default!;

            if (memoryBlockReference.Id == null!)
                memoryBlockReference.Id = memoryReferenceId!;

            return (Input<T>)Activator.CreateInstance(typeof(Input<T>), expression, memoryBlockReference)!;
        }

        var convertedValue = doc.RootElement.ToString().TryConvertTo<T>().Value;
        return (Input<T>)Activator.CreateInstance(typeof(Input<T>), new Literal(convertedValue))!;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Input<T> value, JsonSerializerOptions options)
    {
        var expression = value.Expression;
        var expressionType = expression?.GetType();
        var targetType = value.Type;
        var memoryReferenceId = value.MemoryBlockReference().Id;
        var expressionSyntaxDescriptor = expression != null ? _expressionSyntaxRegistry.Find(x => x.Type == expressionType) : default;

        var model = new
        {
            TypeName = targetType,
            Expression = expressionSyntaxDescriptor?.CreateSerializableObject(new SerializableObjectConstructorContext(expression!)),
            MemoryReference = new
            {
                Id = memoryReferenceId
            }
        };

        JsonSerializer.Serialize(writer, model, options);
    }
}