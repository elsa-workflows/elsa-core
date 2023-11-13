using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Memory;

namespace Elsa.Workflows.Core.Expressions;

/// <summary>
/// Handles Variable expressions.
/// </summary>
public class VariableExpressionHandler : IExpressionHandler
{
    /// <inheritdoc />
    public ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context)
    {
        var variable = expression.Value as Variable;
        var value = variable?.Get(context);
        return ValueTask.FromResult(value);
    }
}