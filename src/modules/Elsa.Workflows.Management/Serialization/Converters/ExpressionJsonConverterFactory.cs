using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;

namespace Elsa.Workflows.Management.Serialization.Converters;

public class ExpressionJsonConverterFactory : JsonConverterFactory
{
    private readonly IExpressionSyntaxRegistry _expressionSyntaxRegistry;
    
    public ExpressionJsonConverterFactory(IExpressionSyntaxRegistry expressionSyntaxRegistry)
    {
        _expressionSyntaxRegistry = expressionSyntaxRegistry;
    }

    // This factory only creates converters when the type to convert is IExpression.
    // The ExpressionJsonConverter will create concrete expression objects, which then uses regular serialization
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(IExpression);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new ExpressionJsonConverter(_expressionSyntaxRegistry);
}