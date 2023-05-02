using Elsa.Expressions.Models;

namespace Elsa.JavaScript.Expressions;

/// <summary>
/// A reference to a JavaScript expression.
/// </summary>
public class JavaScriptExpressionBlockReference : MemoryBlockReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JavaScriptExpressionBlockReference"/> class.
    /// </summary>
    /// <param name="expression">The expression to reference.</param>
    public JavaScriptExpressionBlockReference(JavaScriptExpression expression)
    {
        Expression = expression;
    }
        
    /// <summary>
    /// Gets the referenced expression.
    /// </summary>
    public JavaScriptExpression Expression { get; }
        
    /// <summary>
    /// Declares the referenced expression.
    /// </summary>
    /// <returns>A memory block holding the expression.</returns>
    public override MemoryBlock Declare() => new();
}