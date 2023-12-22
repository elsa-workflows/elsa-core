using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;

namespace Elsa.Expressions.Services;

/// <inheritdoc />
public class ExpressionEvaluator : IExpressionEvaluator
{
    private readonly IExpressionDescriptorRegistry _registry;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionEvaluator"/> class.
    /// </summary>
    public ExpressionEvaluator(IExpressionDescriptorRegistry registry, IServiceProvider serviceProvider)
    {
        _registry = registry;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async ValueTask<T?> EvaluateAsync<T>(Expression expression, ExpressionExecutionContext context, ExpressionEvaluatorOptions? options = default)
    {
        return (T?)await EvaluateAsync(expression, typeof(T), context, options);
    }

    /// <inheritdoc />
    public async ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions? options = default)
    {
        var expressionType = expression.Type;
        var expressionDescriptor = _registry.Find(expressionType);

        if (expressionDescriptor == null)
            throw new Exception($"Could not find an descriptor for expression type \"{expressionType}\".");

        var handler = expressionDescriptor.HandlerFactory(_serviceProvider);
        options ??= new ExpressionEvaluatorOptions();
        return await handler.EvaluateAsync(expression, returnType, context, options);
    }
}