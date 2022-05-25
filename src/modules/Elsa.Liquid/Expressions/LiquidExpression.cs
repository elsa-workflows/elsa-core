using Elsa.Expressions.Services;

namespace Elsa.Liquid.Expressions;

public class LiquidExpression : IExpression
{
    public LiquidExpression(string value) => Value = value;
    public string Value { get; }
}