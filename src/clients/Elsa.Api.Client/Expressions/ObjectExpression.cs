using System.Text.Json.Serialization;
using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Expressions;

/// <summary>
/// Represents an object expression formatted as a string value.
/// </summary>
public class ObjectExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectExpression"/> class.
    /// </summary>
    [JsonConstructor]
    public ObjectExpression()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectExpression"/> class.
    /// </summary>
    public ObjectExpression(string? value)
    {
        Value = value;
    }
    
    /// <summary>
    /// The literal value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Returns the string representation of the expression.
    /// </summary>
    public override string ToString() => Value ?? "";
}