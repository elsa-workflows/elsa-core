using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Expressions.Models;

/// <summary>
/// Represents a literal JSON expression.
/// </summary>
public class ObjectLiteral : MemoryBlockReference
{
    /// <inheritdoc />
    [JsonConstructor]
    public ObjectLiteral()
    {
    }

    /// <inheritdoc />
    public ObjectLiteral(string? value)
    {
        Value = value;
    }
        
    /// <summary>
    /// The literal JSON string value.
    /// </summary>
    public string? Value { get; }

    /// <summary>
    /// Serializes the value into a JSON string in the form of a <see cref="ObjectLiteral{T}"/>
    /// </summary>
    public static ObjectLiteral From<T>(T value) => new ObjectLiteral<T>(value);
}

/// <summary>
/// Represents a JSON string for the specified type <code>T</code>
/// </summary>
public class ObjectLiteral<T> : ObjectLiteral
{
    /// <inheritdoc />
    public ObjectLiteral()
    {
    }

    /// <inheritdoc />
    public ObjectLiteral(T value) : base(JsonSerializer.Serialize(value!))
    {
    }
}