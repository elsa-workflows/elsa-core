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
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <returns>The result of the evaluation.</returns>
    ValueTask<T?> EvaluateAsync<T>(IExpression expression, ExpressionExecutionContext context);
    
    /// <summary>
    /// Evaluates the specified expression and returns the result.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="returnType">The type of the result.</param>
    /// <param name="context">The context in which the expression is evaluated.</param>
    /// <returns>The result of the evaluation.</returns>
    ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context);
}