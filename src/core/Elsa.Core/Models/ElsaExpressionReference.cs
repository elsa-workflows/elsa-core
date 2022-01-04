using Elsa.Expressions;

namespace Elsa.Models;

public class ElsaExpressionReference : RegisterLocationReference
{
    public ElsaExpressionReference(ElsaExpression expression)
    {
        Expression = expression;
    }
        
    public ElsaExpression Expression { get; }
        
    public override RegisterLocation Declare() => new();
}