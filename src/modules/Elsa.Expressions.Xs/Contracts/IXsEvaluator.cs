using Elsa.Expressions.Models;
using JetBrains.Annotations;

namespace Elsa.Expressions.Xs.Contracts;

/// <summary>
/// Evaluates XS expressions.
/// </summary>
[PublicAPI]
public interface IXsEvaluator
{
    /// <summary>
    /// Evaluates an XS expression.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="returnType">The type of the return value.</param>
    /// <param name="context">The context in which the expression is evaluated.</param>
    /// <param name="options">A set of options.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The result of the evaluation.</returns>
    Task<object?> EvaluateAsync(
        string expression, 
        Type returnType, 
        ExpressionExecutionContext context,
        ExpressionEvaluatorOptions options,
        CancellationToken cancellationToken = default);
}
