using Elsa.Expressions.Models;

namespace Elsa.Expressions.Contracts;

public interface IExpressionHandler
{
    ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context);
}