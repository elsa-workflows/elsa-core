using System.Text.Json.Serialization;
using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Expressions;

/// <summary>
/// Represents a c# expression.
/// </summary>
public class CSharpExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpExpression"/> class.
    /// </summary>
    [JsonConstructor]
    public CSharpExpression()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpExpression"/> class.
    /// </summary>
    /// <param name="value">The c# expression.</param>
    public CSharpExpression(string value)
    {
        Value = value;
    }
    
    /// <summary>
    /// Gets or sets the c# expression.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Returns the c# expression.
    /// </summary>
    public override string ToString() => Value ?? "";
}