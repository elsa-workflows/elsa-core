using Elsa.Expressions.Contracts;

namespace Elsa.CSharp.Expressions;

/// <summary>
/// A c# expression.
/// </summary>
public class CSharpExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpExpression"/> class.
    /// </summary>
    /// <param name="value">The c# expression.</param>
    public CSharpExpression(string value)
    {
        Value = value;
    }
        
    /// <summary>
    /// Gets the c# expression.
    /// </summary>
    public string Value { get; }
}