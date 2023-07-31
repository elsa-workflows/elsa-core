using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;

namespace Elsa.Expressions;

/// <summary>
/// An expression handler for <see cref="DelegateExpression"/>.
/// </summary>
public class DelegateExpressionHandler : IExpressionHandler
{
    /// <inheritdoc />
    public async ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var delegateExpression = (DelegateExpression)expression;
        var @delegate = delegateExpression.DelegateBlockReference.Delegate;
        var value = @delegate != null ? await @delegate(context) : default;
        return value;
    }
}