using Elsa.Expressions.Models;
using Jint;

namespace Elsa.JavaScript.Contracts;

public interface IJavaScriptEvaluator
{
    Task<object?> EvaluateAsync(string expression, Type returnType, ExpressionExecutionContext context, Action<Engine>? configureEngine = default, CancellationToken cancellationToken = default);
}