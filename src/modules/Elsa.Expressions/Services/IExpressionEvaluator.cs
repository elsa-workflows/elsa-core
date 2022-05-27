using Elsa.Expressions.Models;

namespace Elsa.Expressions.Services;

public interface IExpressionEvaluator
{
    ValueTask<T?> EvaluateAsync<T>(IExpression input, ExpressionExecutionContext context);
    ValueTask<object?> EvaluateAsync(IExpression input, Type returnType, ExpressionExecutionContext context);
}