using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Management.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Input"/> objects.
/// </summary>
public class InputJsonConverter<T> : JsonConverter<Input<T>>
{
    private readonly IExpressionSyntaxRegistry _expressionSyntaxRegistry;
    private readonly IIdentityGenerator _identityGenerator;

    /// <inheritdoc />
    public InputJsonConverter(IExpressionSyntaxRegistry expressionSyntaxRegistry, IIdentityGenerator identityGenerator)
    {
        _expressionSyntaxRegistry = expressionSyntaxRegistry;
        _identityGenerator = identityGenerator;
    }

    public override bool CanConvert(Type typeToConvert) => typeof(Input).IsAssignableFrom(typeToConvert);

    public override Input<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            return default!;

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return default!;

        if (!doc.RootElement.TryGetProperty("typeName", out var inputTargetTypeElement))
            return default!;

        var memoryReferenceId = doc.RootElement.TryGetProperty("memoryReference", out var memoryReferenceElement) && memoryReferenceElement.ValueKind == JsonValueKind.String
            ? memoryReferenceElement.GetProperty("id").GetString()!
            : _identityGenerator.GenerateId();

        var expressionElement = doc.RootElement.GetProperty("expression");

        if (!expressionElement.TryGetProperty("type", out var expressionTypeNameElement))
            return default!;

        var expressionTypeName = expressionTypeNameElement.GetString() ?? "Literal";
        var expressionSyntaxDescriptor = _expressionSyntaxRegistry.Find(expressionTypeName);

        if (expressionSyntaxDescriptor == null)
            return default!;

        var context = new ExpressionConstructorContext(expressionElement, options);
        var expression = expressionSyntaxDescriptor.CreateExpression(context);
        var locationReference = expressionSyntaxDescriptor.CreateBlockReference(new BlockReferenceConstructorContext(expression, memoryReferenceId));

        return (Input<T>)Activator.CreateInstance(typeof(Input<T>), expression, locationReference)!;
    }

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