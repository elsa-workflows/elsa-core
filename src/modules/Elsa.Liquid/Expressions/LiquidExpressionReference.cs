using Elsa.Expressions.Models;

namespace Elsa.Liquid.Expressions;

public class LiquidExpressionReference : MemoryDatumReference
{
    public LiquidExpressionReference(LiquidExpression expression) => Expression = expression;
    public LiquidExpression Expression { get; }
    public override MemoryDatum Declare() => new();
}