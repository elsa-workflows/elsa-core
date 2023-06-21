using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Expressions;

/// <summary>
/// Represents a JavaScript expression.
/// </summary>
public class JavaScriptExpression : IExpression
{
    /// <summary>
    /// Gets or sets the JavaScript expression.
    /// </summary>
    public string? Value { get; set; } = default!;
}