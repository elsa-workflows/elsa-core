using System.Text.Json.Serialization;
using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Expressions;

/// <summary>
/// Represents a Python expression.
/// </summary>
public class PythonExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PythonExpression"/> class.
    /// </summary>
    [JsonConstructor]
    public PythonExpression()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PythonExpression"/> class.
    /// </summary>
    /// <param name="value">The Python expression.</param>
    public PythonExpression(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets or sets the Python expression.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Returns the Python expression.
    /// </summary>
    public override string ToString() => Value ?? "";
}