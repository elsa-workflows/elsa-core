using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Serialization.Converters;
public class ExpressionConverter : JsonConverter<Expression>
{

    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    public ExpressionConverter(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }

    public override bool CanConvert(Type typeToConvert) => typeof(Expression).IsAssignableFrom(typeToConvert);

    public override Expression? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            return default!;

        var expressionElement = doc.RootElement;
        var expressionTypeNameElement = expressionElement.ValueKind != JsonValueKind.Undefined ? expressionElement.TryGetProperty("type", out var expressionTypeNameElementValue) ? expressionTypeNameElementValue : default : default;
        var expressionTypeName = expressionTypeNameElement.ValueKind != JsonValueKind.Undefined ? expressionTypeNameElement.GetString() ?? "Literal" : default;

        var expression = Activator.CreateInstance<Expression>();
        expression.Type = expressionTypeName;

        if (doc.RootElement.TryGetProperty("value", out var expressionValueElement))
        {
            // Convert known expression types explicitly so they don't remain JsonElements
            expression.Value = _wellKnownTypeRegistry.TryGetType(expressionTypeName, out var expressionValueType) ?
                expressionValueElement.Deserialize(expressionValueType, options) :
                expressionValueElement;
        }

        return expression;
    }

    public override void Write(Utf8JsonWriter writer, Expression value, JsonSerializerOptions options)
    {
        // Write without recursion
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.RemoveWhere(x => x is ExpressionConverter);
        JsonSerializer.Serialize(writer, value, typeof(Expression), newOptions);
    }
}
