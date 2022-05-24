using Elsa.Models;

namespace Elsa.Liquid.Expressions;

public class LiquidExpressionReference : RegisterLocationReference
{
    public LiquidExpressionReference(LiquidExpression expression) => Expression = expression;
    public LiquidExpression Expression { get; }
    public override RegisterLocation Declare() => new();
}