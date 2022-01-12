using System;
using System.Threading.Tasks;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Scripting.JavaScript.Expressions;

public class JavaScriptExpression : IExpression
{
    public JavaScriptExpression(string value)
    {
        Value = value;
    }
        
    public string Value { get; }
}
    
public class JavaScriptExpressionHandler : IExpressionHandler
{
    public ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var javaScriptExpression = (JavaScriptExpression)expression;
            
        // TODO: evaluate expression.
        return new ValueTask<object?>(javaScriptExpression.Value);
    }
}