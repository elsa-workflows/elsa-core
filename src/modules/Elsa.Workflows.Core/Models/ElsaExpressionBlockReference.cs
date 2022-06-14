using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Expressions;

namespace Elsa.Workflows.Core.Models;

public class ElsaExpressionBlockReference : MemoryBlockReference
{
    public ElsaExpressionBlockReference(ElsaExpression expression)
    {
        Expression = expression;
    }
        
    public ElsaExpression Expression { get; }
        
    public override MemoryBlock Declare() => new();
}