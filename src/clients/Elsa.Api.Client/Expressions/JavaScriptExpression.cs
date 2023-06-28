using System.Text.Json.Serialization;
using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Expressions;

/// <summary>
/// Represents a JavaScript expression.
/// </summary>
public class JavaScriptExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JavaScriptExpression"/> class.
    /// </summary>
    [JsonConstructor]
    public JavaScriptExpression()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaScriptExpression"/> class.
    /// </summary>
    /// <param name="value"></param>
    public JavaScriptExpression(string value)
    {
        Value = value;
    }
    
    /// <summary>
    /// Gets or sets the JavaScript expression.
    /// </summary>
    public string? Value { get; set; } = default!;

    /// <summary>
    /// Returns the JavaScript expression.
    /// </summary>
    public override string ToString() => Value ?? "";
}