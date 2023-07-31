using Elsa.Expressions.Models;

namespace Elsa.Expressions.Contracts;

/// <summary>
/// Evaluates an expression.
/// </summary>
public interface IExpressionHandler
{
    /// <summary>
    /// Evaluates an expression.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="returnType">The expected return type.</param>
    /// <param name="context">The context in which the expression is evaluated.</param>
    /// <returns>The result of the evaluation.</returns>
    ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context);
}