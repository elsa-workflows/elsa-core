using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;

namespace Elsa.Expressions.Services;

public class ExpressionEvaluator : IExpressionEvaluator
{
    private readonly IExpressionHandlerRegistry _registry;

    public ExpressionEvaluator(IExpressionHandlerRegistry registry)
    {
        _registry = registry;
    }

    public async ValueTask<T?> EvaluateAsync<T>(IExpression expression, ExpressionExecutionContext context) => (T?)await EvaluateAsync(expression, typeof(T), context);

    public async ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var handler = _registry.GetHandler(expression);

        if (handler != null)
            return await handler.EvaluateAsync(expression, returnType, context);
        
        throw new InvalidOperationException($"Could not find handler for expression type {expression.GetType().FullName}");
    }
}