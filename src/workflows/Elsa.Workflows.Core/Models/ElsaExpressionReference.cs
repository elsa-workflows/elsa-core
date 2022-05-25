using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Expressions;

namespace Elsa.Workflows.Core.Models;

public class ElsaExpressionReference : RegisterLocationReference
{
    public ElsaExpressionReference(ElsaExpression expression)
    {
        Expression = expression;
    }
        
    public ElsaExpression Expression { get; }
        
    public override RegisterLocation Declare() => new();
}