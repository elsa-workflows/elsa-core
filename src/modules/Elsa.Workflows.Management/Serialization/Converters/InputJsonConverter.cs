using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Management.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Input"/> objects.
/// </summary>
public class InputJsonConverter<T> : JsonConverter<Input<T>>
{
    private readonly IExpressionSyntaxRegistry _expressionSyntaxRegistry;

    public InputJsonConverter(IExpressionSyntaxRegistry expressionSyntaxRegistry)
    {
        _expressionSyntaxRegistry = expressionSyntaxRegistry;
    }

    public override bool CanConvert(Type typeToConvert) => typeof(Input).IsAssignableFrom(typeToConvert);

    public override Input<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            return default!;

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return default!;
        
        if (!doc.RootElement.TryGetProperty("type", out var inputTargetTypeElement))
            return default!;

        var expressionElement = doc.RootElement.GetProperty("expression");

        if (!expressionElement.TryGetProperty("type", out var expressionTypeNameElement))
            return default!;

        var expressionTypeName = expressionTypeNameElement.GetString() ?? "Literal";
        var expressionSyntaxDescriptor = _expressionSyntaxRegistry.Find(expressionTypeName);

        if (expressionSyntaxDescriptor == null)
            return default!;

        var context = new ExpressionConstructorContext(expressionElement, options);
        var expression = expressionSyntaxDescriptor.CreateExpression(context);
        var locationReference = expressionSyntaxDescriptor.CreateLocationReference(new LocationReferenceConstructorContext(expression));

        return (Input<T>)Activator.CreateInstance(typeof(Input<T>), expression, locationReference)!;
    }

    public override void Write(Utf8JsonWriter writer, Input<T> value, JsonSerializerOptions options)
    {
        var expression = value.Expression;
        var expressionType = expression.GetType();
        var targetType = value.Type;
        var expressionSyntaxDescriptor = _expressionSyntaxRegistry.Find(x => x.Type == expressionType);

        if (expressionSyntaxDescriptor == null)
            throw new Exception($"Syntax descriptor with expression type {expressionType} not found in registry");

        var model = new
        {
            Type = targetType,
            Expression = expressionSyntaxDescriptor.CreateSerializableObject(new SerializableObjectConstructorContext(expression))
        };

        JsonSerializer.Serialize(writer, model, options);
    }
}