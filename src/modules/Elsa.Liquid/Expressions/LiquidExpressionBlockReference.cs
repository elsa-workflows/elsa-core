using Elsa.Expressions.Models;

namespace Elsa.Liquid.Expressions;

public class LiquidExpressionBlockReference : MemoryBlockReference
{
    public LiquidExpressionBlockReference(LiquidExpression expression) => Expression = expression;
    public LiquidExpression Expression { get; }
    public override MemoryBlock Declare() => new();
}