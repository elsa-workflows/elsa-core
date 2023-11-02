using Elsa.Expressions.Models;

namespace Elsa.CSharp.Expressions;

/// <summary>
/// A reference to a C# expression.
/// </summary>
public class CSharpExpressionBlockReference : MemoryBlockReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpExpressionBlockReference"/> class.
    /// </summary>
    /// <param name="expression">The expression to reference.</param>
    public CSharpExpressionBlockReference(CSharpExpression expression)
    {
        Expression = expression;
    }
        
    /// <summary>
    /// Gets the referenced expression.
    /// </summary>
    public CSharpExpression Expression { get; }
        
    /// <summary>
    /// Declares the referenced expression.
    /// </summary>
    /// <returns>A memory block holding the expression.</returns>
    public override MemoryBlock Declare() => new();
}