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

            var expressionElement = doc.RootElement.GetProperty("expression");

            if (!expressionElement.TryGetProperty("type", out var expressionTypeNameElement))
                return default!;

            if (!expressionElement.TryGetProperty("value", out _))
                return default!;

            var expressionTypeName = expressionTypeNameElement.GetString() ?? "Literal";
            var expressionSyntaxDescriptor = _expressionSyntaxRegistry.Find(expressionTypeName);

            if (expressionSyntaxDescriptor == null)
                return default!;

            doc.RootElement.TryGetProperty("memoryReference", out var memoryReferenceElement);

            var memoryReferenceId = memoryReferenceElement.ValueKind == JsonValueKind.Undefined
                ? default
                : memoryReferenceElement.TryGetProperty("id", out var memoryReferenceIdElement)
                    ? memoryReferenceIdElement.GetString()
                    : default;

            var context = new ExpressionConstructorContext(expressionElement, options);
            var expression = expressionSyntaxDescriptor.CreateExpression(context);
            var memoryBlockReference = expressionSyntaxDescriptor.CreateBlockReference(new BlockReferenceConstructorContext(expression));

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
        var expressionType = expression.GetType();
        var targetType = value.Type;
        var memoryReferenceId = value.MemoryBlockReference().Id;
        var expressionSyntaxDescriptor = _expressionSyntaxRegistry.Find(x => x.Type == expressionType);

        if (expressionSyntaxDescriptor == null)
            throw new Exception($"Syntax descriptor with expression type {expressionType} not found in registry");

        var model = new
        {
            TypeName = targetType,
            Expression = expressionSyntaxDescriptor.CreateSerializableObject(new SerializableObjectConstructorContext(expression)),
            MemoryReference = new
            {
                Id = memoryReferenceId
            }
        };

        JsonSerializer.Serialize(writer, model, options);
    }
}