using Elsa.Models;
using Jint;

namespace Elsa.Scripting.JavaScript.Services
{
    public interface IJavaScriptEvaluator
    {
        Task<object?> EvaluateAsync(string expression, Type returnType, ExpressionExecutionContext context, Action<Engine>? configureEngine = default, CancellationToken cancellationToken = default);
    }
}