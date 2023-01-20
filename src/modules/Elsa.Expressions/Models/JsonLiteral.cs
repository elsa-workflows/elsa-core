using System.Text.Json;

namespace Elsa.Expressions.Models;

public class JsonLiteral : MemoryBlockReference
{
    public JsonLiteral()
    {
    }

    public JsonLiteral(string? value)
    {
        Value = value;
    }
        
    public string? Value { get; }
    public override MemoryBlock Declare() => new();

    public static JsonLiteral From<T>(T value) => new JsonLiteral<T>(value);
}

public class JsonLiteral<T> : JsonLiteral
{
    public JsonLiteral()
    {
    }

    public JsonLiteral(T value) : base(JsonSerializer.Serialize(value!))
    {
    }
}