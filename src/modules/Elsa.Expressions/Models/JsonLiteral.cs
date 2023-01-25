using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Expressions.Models;

/// <summary>
/// Represents a literal JSON expression.
/// </summary>
public class JsonLiteral : MemoryBlockReference
{
    /// <inheritdoc />
    [JsonConstructor]
    public JsonLiteral()
    {
    }

    /// <inheritdoc />
    public JsonLiteral(string? value)
    {
        Value = value;
    }
        
    /// <summary>
    /// The literal JSON string value.
    /// </summary>
    public string? Value { get; }
    
    /// <inheritdoc />
    public override MemoryBlock Declare() => new();

    /// <summary>
    /// Serializes the value into a JSON string in the form of a <see cref="JsonLiteral{T}"/>
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static JsonLiteral From<T>(T value) => new JsonLiteral<T>(value);
}

/// <summary>
/// Represents a JSON string for the specified type <code>T</code>
/// </summary>
public class JsonLiteral<T> : JsonLiteral
{
    /// <inheritdoc />
    public JsonLiteral()
    {
    }

    /// <inheritdoc />
    public JsonLiteral(T value) : base(JsonSerializer.Serialize(value!))
    {
    }
}