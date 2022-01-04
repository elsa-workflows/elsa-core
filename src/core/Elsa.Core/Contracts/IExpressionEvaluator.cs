using Elsa.Models;

namespace Elsa.Contracts;

public interface IExpressionEvaluator
{
    ValueTask<T?> EvaluateAsync<T>(IExpression input, ExpressionExecutionContext context);
    ValueTask<object?> EvaluateAsync(IExpression input, Type returnType, ExpressionExecutionContext context);
}

public static class ExpressionEvaluatorExtensions
{
    public static ValueTask<T?> EvaluateAsync<T>(this IExpressionEvaluator evaluator, Input<T> input, ExpressionExecutionContext context) => evaluator.EvaluateAsync<T>(input.Expression, context);
    public static ValueTask<object?> EvaluateAsync(this IExpressionEvaluator evaluator, Input input, ExpressionExecutionContext context) => evaluator.EvaluateAsync(input.Expression, input.Type, context);
}