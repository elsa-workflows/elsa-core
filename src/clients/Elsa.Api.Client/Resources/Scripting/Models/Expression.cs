using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Resources.Scripting.Models;

/// <summary>
/// Represents a dynamic expression.
/// </summary>
public class Expression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Expression"/> class.
    /// </summary>
    [JsonConstructor]
    public Expression()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Expression"/> class.
    /// </summary>
    /// <param name="type">The type of the expression.</param>
    /// <param name="value">The expression.</param>
    public Expression(string type, string? value = default)
    {
        Type = type;
        Value = value;
    }
    
    /// <summary>
    /// Gets or sets the expression type.
    /// </summary>
    public string Type { get; set; } = default!;

    /// <summary>
    /// Gets or sets the C# expression.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Returns the C# expression.
    /// </summary>
    public override string ToString() => Value ?? "";
    
    /// <summary>
    /// Creates a literal expression.
    /// </summary>
    public static Expression CreateLiteral(string value) => new Expression("Literal", value);
    
    /// <summary>
    /// Creates an object expression.
    /// </summary>
    public static Expression CreateObject(string value) => new Expression("Object", value);
}