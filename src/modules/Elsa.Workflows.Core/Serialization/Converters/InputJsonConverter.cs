using System.Diagnostics.CodeAnalysis;
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
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
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
            var expressionDescriptor = expressionTypeName != null ? _expressionDescriptorRegistry.Find(expressionTypeName) : default;
            var memoryBlockReference = expressionDescriptor?.MemoryBlockReferenceFactory?.Invoke();

            if (memoryBlockReference == null)
                return default!;

            var memoryBlockType = memoryBlockReference.GetType();
            var context = new ExpressionSerializationContext(expressionTypeName!, expressionElement, options, memoryBlockType);
            var expression = expressionDescriptor!.Deserialize(context);
            return (Input<T>)Activator.CreateInstance(typeof(Input<T>), expression, memoryBlockReference)!;
        }

        var convertedValue = doc.RootElement.ToString().TryConvertTo<T>().Value;
        return (Input<T>)Activator.CreateInstance(typeof(Input<T>), new Literal(convertedValue))!;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Input<T> value, JsonSerializerOptions options)
    {
        var expression = value.Expression;

        if (expression == null)
        {
            writer.WriteNullValue();
            return;
        }

        var expressionType = expression.Type;
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