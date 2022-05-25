using Elsa.Expressions.Services;

namespace Elsa.JavaScript.Expressions;

public class JavaScriptExpression : IExpression
{
    public JavaScriptExpression(string value)
    {
        Value = value;
    }
        
    public string Value { get; }
}