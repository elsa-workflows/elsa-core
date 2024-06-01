using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Expression"/> objects.
/// </summary>
public class ExpressionJsonConverter : JsonConverter<Expression>
{
    private readonly IExpressionDescriptorRegistry _expressionDescriptorRegistry;

    /// <inheritdoc />
    public ExpressionJsonConverter(IExpressionDescriptorRegistry expressionDescriptorRegistry)
    {
        _expressionDescriptorRegistry = expressionDescriptorRegistry;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(Input).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override Expression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            return default!;

        var expressionElement = doc.RootElement;
        var expressionTypeNameElement = expressionElement.TryGetProperty("type", out var expressionTypeNameElementValue) ? expressionTypeNameElementValue : default;
        var expressionTypeName = expressionTypeNameElement.ValueKind != JsonValueKind.Undefined ? expressionTypeNameElement.GetString() ?? "Literal" : default;
        var expressionDescriptor = expressionTypeName != null ? _expressionDescriptorRegistry.Find(expressionTypeName) : default;
        var memoryBlockReference = expressionDescriptor?.MemoryBlockReferenceFactory?.Invoke();

        if (memoryBlockReference == null)
            return default!;

        var memoryBlockType = memoryBlockReference.GetType();
        var context = new ExpressionSerializationContext(expressionTypeName!, expressionElement, options, memoryBlockType);
        var expression = expressionDescriptor!.Deserialize(context);

        return expression;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Expression value, JsonSerializerOptions options)
    {
        var expression = value;
        
        if(expression == null)
        {
            writer.WriteNullValue();
            return;
        }
        
        var expressionType = expression.Type;
        var expressionDescriptor = expressionType != null ? _expressionDescriptorRegistry.Find(expressionType) : null;

        if (expressionDescriptor == null)
            throw new JsonException($"Could not find an expression descriptor for expression type '{expressionType}'.");
        
        var expressionValue = expressionDescriptor.IsSerializable ? expression.Value : null;

        var model = new
        {
            Type = expressionType,
            Value = expressionValue
        };
        
        JsonSerializer.Serialize(writer, model, options);
    }
}