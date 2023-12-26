using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Expressions;

/// <summary>
/// Evaluates an object expression.
/// </summary>
public class ObjectExpressionHandler : IExpressionHandler
{
    /// <inheritdoc />
    public ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        var value = expression.Value.ConvertTo<string>() ?? "";

        if (string.IsNullOrWhiteSpace(value))
            return ValueTask.FromResult(default(object?));

        var serializerOptions = new JsonSerializerOptions();
        serializerOptions.Converters.Add(new IntegerConverter());
        
        var converterOptions = new ObjectConverterOptions(serializerOptions);
        var model = value.ConvertTo(returnType, converterOptions);
        return ValueTask.FromResult(model);
    }
}