using Elsa.Expressions.Models;

namespace Elsa.JavaScript.Expressions;

public class JavaScriptExpressionReference : MemoryDatumReference
{
    public JavaScriptExpressionReference(JavaScriptExpression expression)
    {
        Expression = expression;
    }
        
    public JavaScriptExpression Expression { get; }
        
    public override MemoryDatum Declare() => new();
}