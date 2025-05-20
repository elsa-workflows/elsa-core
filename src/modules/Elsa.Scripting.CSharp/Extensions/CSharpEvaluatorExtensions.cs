using Elsa.Scripting.CSharp.Contracts;
using Elsa.Expressions.Models;

namespace Elsa.Scripting.CSharp.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ICSharpEvaluator"/>.
/// </summary>
public static class CSharpEvaluatorExtensions
{
    /// <summary>
    /// Evaluates a C# expression.
    /// </summary>
    /// <param name="evaluator">The evaluator.</param>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="returnType">The type of the return value.</param>
    /// <param name="context">The context in which the expression is evaluated.</param>
    /// <param name="options">An set of options.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The result of the evaluation.</returns>
    public static async Task<object?> EvaluateAsync(this ICSharpEvaluator evaluator,
        string expression,
        Type returnType,
        ExpressionExecutionContext context,
        ExpressionEvaluatorOptions options,
        CancellationToken cancellationToken = default)
    {
        return await evaluator.EvaluateAsync(expression, returnType, context, options, null, null, cancellationToken);
    }
}