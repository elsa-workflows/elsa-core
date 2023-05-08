using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;

namespace Elsa.Workflows.Management.Serialization.Converters;

/// <summary>
/// A JSON converter factory that creates <see cref="ExpressionJsonConverter"/> instances.
/// </summary>
public class ExpressionJsonConverterFactory : JsonConverterFactory
{
    private readonly IExpressionSyntaxRegistry _expressionSyntaxRegistry;

    /// <inheritdoc />
    public ExpressionJsonConverterFactory(IExpressionSyntaxRegistry expressionSyntaxRegistry)
    {
        _expressionSyntaxRegistry = expressionSyntaxRegistry;
    }

    // This factory only creates converters when the type to convert is IExpression.
    // The ExpressionJsonConverter will create concrete expression objects, which then uses regular serialization
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(IExpression);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new ExpressionJsonConverter(_expressionSyntaxRegistry);
}