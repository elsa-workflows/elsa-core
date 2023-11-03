using Elsa.Expressions.Models;

namespace Elsa.Python.Expressions;

/// <summary>
/// A reference to a Python expression.
/// </summary>
public class PythonExpressionBlockReference : MemoryBlockReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PythonExpressionBlockReference"/> class.
    /// </summary>
    /// <param name="expression">The expression to reference.</param>
    public PythonExpressionBlockReference(PythonExpression expression)
    {
        Expression = expression;
    }
        
    /// <summary>
    /// Gets the referenced expression.
    /// </summary>
    public PythonExpression Expression { get; }
        
    /// <summary>
    /// Declares the referenced expression.
    /// </summary>
    /// <returns>A memory block holding the expression.</returns>
    public override MemoryBlock Declare() => new();
}