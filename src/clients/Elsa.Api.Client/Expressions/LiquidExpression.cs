using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Expressions;

/// <summary>
/// Represents a Liquid expression.
/// </summary>
public class LiquidExpression : IExpression
{
    /// <summary>
    /// The Liquid expression.
    /// </summary>
    public string? Value { get; set; } = default!;
}