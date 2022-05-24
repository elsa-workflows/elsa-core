using Elsa.Models;

namespace Elsa.Services;

public interface IExpressionHandler
{
    ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context);
}