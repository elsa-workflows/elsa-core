using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Expressions;

namespace Elsa.Api.Client.Converters;

/// <summary>
/// A JSON converter that serializes <see cref="IExpression"/>.
/// </summary>
public class ExpressionJsonConverter : JsonConverter<IExpression>
{
    /// <inheritdoc />
    public override IExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");
        
        var expressionElement = doc.RootElement;
        var expressionTypeName = expressionElement.GetProperty("type").GetString()!;
        var expressionType = GetExpressionType(expressionTypeName);
        var newOptions = new JsonSerializerOptions(options);
        var expression = (IExpression)JsonSerializer.Deserialize(expressionElement.GetRawText(), expressionType, newOptions)!;
      
        return expression;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IExpression value, JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        newOptions.Converters.Add(new JsonStringEnumConverter());

        // Write to a JsonObject so that we can add additional information.
        var expressionType = value.GetType();
        var expressionModel = JsonSerializer.SerializeToNode(value, expressionType, newOptions)!;
        var typeName = GetExpressionTypeName(expressionType);
        
        expressionModel["type"] = typeName;
        expressionModel.WriteTo(writer, newOptions);
    }
    
    // TODO: Make this extensible.
    private Type GetExpressionType(string expressionTypeName) =>
        expressionTypeName switch
        {
            "Literal" => typeof(LiteralExpression),
            "JavaScript" => typeof(JavaScriptExpression),
            "Liquid" => typeof(LiquidExpression),
            "Object" => typeof(ObjectExpression),
            _ => throw new ArgumentOutOfRangeException(nameof(expressionTypeName), expressionTypeName, null)
        };
    
    private string GetExpressionTypeName(Type expressionType) =>
        expressionType.Name switch
        {
            nameof(LiteralExpression) => "Literal",
            nameof(JavaScriptExpression) => "JavaScript",
            nameof(LiquidExpression) => "Liquid",
            nameof(ObjectExpression) => "Object",
            _ => throw new ArgumentOutOfRangeException(nameof(expressionType), expressionType, null)
        };
}