using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Services;

public class ExpressionEvaluator : IExpressionEvaluator
{
    private readonly IExpressionHandlerRegistry _registry;

    public ExpressionEvaluator(IExpressionHandlerRegistry registry)
    {
        _registry = registry;
    }

    public async ValueTask<T?> EvaluateAsync<T>(IExpression input, ExpressionExecutionContext context) => (T?)await EvaluateAsync(input, typeof(T), context);

    public async ValueTask<object?> EvaluateAsync(IExpression input, Type returnType, ExpressionExecutionContext context)
    {
        var handler = _registry.GetHandler(input);

        if (handler != null)
            return await handler.EvaluateAsync(input, returnType, context);

        var expressionType = input.GetType().GetGenericTypeDefinition();
        throw new InvalidOperationException($"Could not find handler for expression type {expressionType}");
    }
}