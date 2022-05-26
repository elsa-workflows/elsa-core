using Elsa.Expressions.Models;

namespace Elsa.JavaScript.Expressions;

public class JavaScriptExpressionReference : RegisterLocationReference
{
    public JavaScriptExpressionReference(JavaScriptExpression expression)
    {
        Expression = expression;
    }
        
    public JavaScriptExpression Expression { get; }
        
    public override RegisterLocation Declare() => new();
}