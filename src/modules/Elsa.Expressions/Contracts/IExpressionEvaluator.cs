using Elsa.Expressions.Models;

namespace Elsa.Expressions.Contracts;

public interface IExpressionEvaluator
{
    ValueTask<T?> EvaluateAsync<T>(IExpression input, ExpressionExecutionContext context);
    ValueTask<object?> EvaluateAsync(IExpression input, Type returnType, ExpressionExecutionContext context);
}