using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ExpressionEvaluatorExtensions
{
    public static ValueTask<T?> EvaluateAsync<T>(this IExpressionEvaluator evaluator, Input<T> input, ExpressionExecutionContext context) => evaluator.EvaluateAsync<T>(input.Expression, context);
    public static ValueTask<object?> EvaluateAsync(this IExpressionEvaluator evaluator, Input input, ExpressionExecutionContext context) => evaluator.EvaluateAsync(input.Expression, input.Type, context);
}