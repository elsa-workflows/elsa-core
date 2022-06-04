using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Expressions;

namespace Elsa.Workflows.Core.Models;

public class ElsaExpressionReference : MemoryReference
{
    public ElsaExpressionReference(ElsaExpression expression)
    {
        Expression = expression;
    }
        
    public ElsaExpression Expression { get; }
        
    public override MemoryBlock Declare() => new();
}