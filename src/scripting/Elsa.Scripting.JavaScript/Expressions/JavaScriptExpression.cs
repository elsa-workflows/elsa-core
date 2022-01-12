using System;
using System.Threading.Tasks;
using Elsa.Contracts;

namespace Elsa.Scripting.JavaScript.Expressions;

public class JavaScriptExpression : IExpression
{
    public JavaScriptExpression(string value)
    {
        Value = value;
    }
        
    public string Value { get; }
}