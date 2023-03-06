using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;

namespace Elsa.Workflows.Management.Serialization.Converters;

/// <summary>
/// (De)serializes objects of type <see cref="IExpression"/>.
/// </summary>
public class ExpressionJsonConverter : JsonConverter<IExpression>
{
    private readonly IExpressionSyntaxRegistry _expressionSyntaxRegistry;

    /// <inheritdoc />
    public ExpressionJsonConverter(IExpressionSyntaxRegistry expressionSyntaxRegistry)
    {
        _expressionSyntaxRegistry = expressionSyntaxRegistry;
    }

    /// <inheritdoc />
    public override IExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        if (!doc.RootElement.TryGetProperty("type", out var syntaxElement))
            throw new JsonException("Failed to extract expression type property");

        var syntax = syntaxElement.GetString()!;
        var expressionSyntaxDescriptor = _expressionSyntaxRegistry.Find(syntax);

        if (expressionSyntaxDescriptor == null)
            throw new Exception($"Expression with syntax {syntax} not found in registry");
        
        var context = new ExpressionConstructorContext(doc.RootElement, options);
        var expression = expressionSyntaxDescriptor.CreateExpression(context);

        return expression;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IExpression value, JsonSerializerOptions options)
    {
        var expressionType = value.GetType();
        var descriptor = _expressionSyntaxRegistry.Find(x => x.Type == expressionType);
        
        if(descriptor == null)
            throw new Exception($"Expression of type {expressionType} not found in registry");

        var model = descriptor.CreateSerializableObject(new SerializableObjectConstructorContext(value));

        JsonSerializer.Serialize(writer, model, options);
    }
}