using Elsa.Expressions.Contracts;

namespace Elsa.JavaScript.Expressions;

/// <summary>
/// A JavaScript expression.
/// </summary>
public class JavaScriptExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JavaScriptExpression"/> class.
    /// </summary>
    /// <param name="value">The JavaScript expression.</param>
    public JavaScriptExpression(string value)
    {
        Value = value;
    }
        
    /// <summary>
    /// Gets the JavaScript expression.
    /// </summary>
    public string Value { get; }
}