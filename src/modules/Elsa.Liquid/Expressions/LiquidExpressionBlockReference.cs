using Elsa.Expressions.Models;

namespace Elsa.Liquid.Expressions;

/// <summary>
/// A reference to a <see cref="LiquidExpression"/> block.
/// </summary>
public class LiquidExpressionBlockReference : MemoryBlockReference
{
    /// <summary>
    /// Creates a new instance of the <see cref="LiquidExpressionBlockReference"/> class.
    /// </summary>
    public LiquidExpressionBlockReference(LiquidExpression expression) => Expression = expression;
    
    /// <summary>
    /// Gets the <see cref="LiquidExpression"/>.
    /// </summary>
    public LiquidExpression Expression { get; }
    
    /// <summary>
    /// Declares a new <see cref="MemoryBlock"/>.
    /// </summary>
    /// <returns></returns>
    public override MemoryBlock Declare() => new();
}