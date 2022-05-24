using Elsa.Models;

namespace Elsa.Scripting.JavaScript.Expressions;

public class JavaScriptExpressionReference : RegisterLocationReference
{
    public JavaScriptExpressionReference(JavaScriptExpression expression)
    {
        Expression = expression;
    }
        
    public JavaScriptExpression Expression { get; }
        
    public override RegisterLocation Declare() => new();
}