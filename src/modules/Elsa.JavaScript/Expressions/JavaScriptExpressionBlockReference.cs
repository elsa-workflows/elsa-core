using Elsa.Expressions.Models;

namespace Elsa.JavaScript.Expressions;

public class JavaScriptExpressionBlockReference : MemoryBlockReference
{
    public JavaScriptExpressionBlockReference(JavaScriptExpression expression)
    {
        Expression = expression;
    }
        
    public JavaScriptExpression Expression { get; }
        
    public override MemoryBlock Declare() => new();
}