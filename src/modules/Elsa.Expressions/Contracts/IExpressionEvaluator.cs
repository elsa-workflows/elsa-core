using Elsa.Expressions.Models;

namespace Elsa.Expressions.Contracts;

/// <summary>
/// Evaluates expressions.
/// </summary>
public interface IExpressionEvaluator
{
    /// <summary>
    /// Evaluates the specified expression and returns the result.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="context">The context in which the expression is evaluated.</param>
    /// <param name="options">An optional set of options.</param>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <returns>The result of the evaluation.</returns>
    ValueTask<T?> EvaluateAsync<T>(Expression expression, ExpressionExecutionContext context, ExpressionEvaluatorOptions? options = default);

    /// <summary>
    /// Evaluates the specified expression and returns the result.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="returnType">The type of the result.</param>
    /// <param name="context">The context in which the expression is evaluated.</param>
    /// <param name="options">An optional set of options.</param>
    /// <returns>The result of the evaluation.</returns>
    ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions? options = default);
}