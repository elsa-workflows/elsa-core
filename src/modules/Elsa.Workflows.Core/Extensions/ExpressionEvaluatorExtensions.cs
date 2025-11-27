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
    extension(IExpressionEvaluator evaluator)
    {
        /// <summary>
        /// Evaluates the specified expression and returns the result.
        /// </summary>
        public ValueTask<T?> EvaluateAsync<T>(Input<T> input, ExpressionExecutionContext context, ExpressionEvaluatorOptions? options = default)
        {
            return evaluator.EvaluateAsync<T>(input.Expression!, context, options);
        }

        /// <summary>
        /// Evaluates the specified expression and returns the result.
        /// </summary>
        public ValueTask<object?> EvaluateAsync(Input input, ExpressionExecutionContext context, ExpressionEvaluatorOptions? options = default)
        {
            return evaluator.EvaluateAsync(input.Expression!, input.Type, context, options);
        }
    }
}