using Elsa.Expressions.Models;
using JetBrains.Annotations;
using Jint;

namespace Elsa.Scripting.JavaScript.Contracts;

/// <summary>
/// Evaluates JavaScript expressions.
/// </summary>
[PublicAPI]
public interface IJavaScriptEvaluator
{
    /// <summary>
    /// Evaluates a JavaScript expression.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="returnType">The type of the return value.</param>
    /// <param name="context">The context in which the expression is evaluated.</param>
    /// <param name="options">The options to use when evaluating the expression.</param>
    /// <param name="configureEngine">An optional callback that can be used to configure the JavaScript engine.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The result of the evaluation.</returns>
    Task<object?> EvaluateAsync(
        string expression, 
        Type returnType, 
        ExpressionExecutionContext context,
        ExpressionEvaluatorOptions? options = default,
        Action<Engine>? configureEngine = default, 
        CancellationToken cancellationToken = default);
}