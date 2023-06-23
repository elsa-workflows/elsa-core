using System.Text.Json.Serialization;
using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Expressions;

/// <summary>
/// Represents a literal expression.
/// </summary>
public class LiteralExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LiteralExpression"/> class.
    /// </summary>
    [JsonConstructor]
    public LiteralExpression()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LiteralExpression"/> class.
    /// </summary>
    public LiteralExpression(object? value)
    {
        Value = value;
    }
    
    /// <summary>
    /// The literal value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Returns the string representation of the expression.
    /// </summary>
    public override string ToString() => Value?.ToString() ?? "";
}