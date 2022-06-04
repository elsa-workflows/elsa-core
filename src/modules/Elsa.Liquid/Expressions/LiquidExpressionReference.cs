using Elsa.Expressions.Models;

namespace Elsa.Liquid.Expressions;

public class LiquidExpressionReference : MemoryReference
{
    public LiquidExpressionReference(LiquidExpression expression) => Expression = expression;
    public LiquidExpression Expression { get; }
    public override MemoryBlock Declare() => new();
}