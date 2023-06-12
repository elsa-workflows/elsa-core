using Elsa.Expressions.Contracts;

namespace Elsa.Liquid.Expressions;

/// <summary>
/// Represents a Liquid expression.
/// </summary>
public class LiquidExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LiquidExpression"/> class.
    /// </summary>
    /// <param name="value">The Liquid expression.</param>
    public LiquidExpression(string value) => Value = value;
    
    /// <summary>
    /// Gets the Liquid expression.
    /// </summary>
    public string Value { get; }
}