using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Contains extension methods for <see cref="IExpressionEvaluator"/>.
/// </summary>
public static class ExpressionEvaluatorExtensions
{
    /// <summary>
    /// Evaluates the specified expression and returns the result.
    /// </summary>
    public static ValueTask<T?> EvaluateAsync<T>(this IExpressionEvaluator evaluator, Input<T> input, ExpressionExecutionContext context, ExpressionEvaluatorOptions? options = default)
    {
        return evaluator.EvaluateAsync<T>(input.Expression!, context, options);
    }

    /// <summary>
    /// Evaluates the specified expression and returns the result.
    /// </summary>
    public static ValueTask<object?> EvaluateAsync(this IExpressionEvaluator evaluator, Input input, ExpressionExecutionContext context, ExpressionEvaluatorOptions? options = default)
    {
        return evaluator.EvaluateAsync(input.Expression!, input.Type, context, options);
    }
}