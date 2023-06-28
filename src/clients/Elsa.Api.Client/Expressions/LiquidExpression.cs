using System.Text.Json.Serialization;
using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Expressions;

/// <summary>
/// Represents a Liquid expression.
/// </summary>
public class LiquidExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LiquidExpression"/> class.
    /// </summary>
    [JsonConstructor]
    public LiquidExpression()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="LiquidExpression"/> class.
    /// </summary>
    public LiquidExpression(string value)
    {
        Value = value;
    }
    
    /// <summary>
    /// The Liquid expression.
    /// </summary>
    public string? Value { get; set; } = default!;
    
    /// <summary>
    /// Returns the Liquid expression.
    /// </summary>
    public override string ToString() => Value ?? "";
}