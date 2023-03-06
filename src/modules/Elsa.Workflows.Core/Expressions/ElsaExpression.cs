using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Expressions;

public class ElsaExpression : IExpression
{
    public ElsaExpression(string value)
    {
        Value = value;
    }
        
    public string Value { get; }
}
    
public class ElsaExpressionHandler : IExpressionHandler
{
    public ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var elsaExpression = (ElsaExpression)expression;
            
        // TODO: evaluate expression.
        return new ValueTask<object?>(elsaExpression.Value);
    }
}