using Elsa.Expressions.Models;
using Microsoft.PowerFx;

namespace Elsa.Expressions.PowerFx.Contracts;

/// <summary>
/// Evaluates Power Fx expressions.
/// </summary>
public interface IPowerFxEvaluator
{
    /// <summary>
    /// Evaluates a Power Fx expression.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="returnType">The expected return type.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="options">Additional options for the evaluator.</param>
    /// <param name="configureEngine">A delegate to configure the Power Fx engine.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the evaluation.</returns>
    ValueTask<object?> EvaluateAsync(
        string expression, 
        Type returnType, 
        ExpressionExecutionContext context, 
        ExpressionEvaluatorOptions options, 
        Action<RecalcEngine>? configureEngine = default, 
        CancellationToken cancellationToken = default);
}