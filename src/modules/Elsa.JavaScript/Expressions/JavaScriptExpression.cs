using Elsa.Services;

namespace Elsa.Scripting.JavaScript.Expressions;

public class JavaScriptExpression : IExpression
{
    public JavaScriptExpression(string value)
    {
        Value = value;
    }
        
    public string Value { get; }
}