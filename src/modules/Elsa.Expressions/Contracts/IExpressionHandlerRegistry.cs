namespace Elsa.Expressions.Contracts;

/// <summary>
/// A registry of expression handlers.
/// </summary>
public interface IExpressionHandlerRegistry
{
    /// <summary>
    /// Registers an expression handler for the specified expression type.
    /// </summary>
    /// <param name="expressionType">The expression type.</param>
    /// <param name="handler">The expression handler type.</param>
    void Register(Type expressionType, Type handler);
    
    /// <summary>
    /// Returns an expression handler for the specified expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns>An expression handler or <c>null</c> if no handler was found.</returns>
    IExpressionHandler? GetHandler(IExpression expression);
}