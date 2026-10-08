using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Expressions;

/// <summary>
/// Evaluates an <see cref="Output"/> expression.
/// </summary>
public class OutputExpressionHandler : IExpressionHandler
{
    /// <inheritdoc />
    public ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        var value = expression.Value is Output output ? context.Get(output) : default;
        return ValueTask.FromResult(value);
    }
}