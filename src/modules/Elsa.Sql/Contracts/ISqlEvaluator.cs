using Elsa.Expressions.Models;
using Elsa.Sql.Models;
using JetBrains.Annotations;

namespace Elsa.Sql.Contracts;

/// <summary>
/// Evaluates SQL expressions.
/// </summary>
[PublicAPI]
public interface ISqlEvaluator
{
    /// <summary>
    /// Evaluates a SQL expression.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="context">The context in which the expression is evaluated.</param>
    /// <param name="options">A set of options.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The <see cref="EvaluatedQuery"/> result.</returns>
    Task<EvaluatedQuery> EvaluateAsync(
        string expression,
        ExpressionExecutionContext context,
        ExpressionEvaluatorOptions options,
        CancellationToken cancellationToken = default);
}