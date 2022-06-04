namespace Elsa.Expressions.Models;

/// <summary>
/// Represents a piece of memory within a memory register
/// </summary>
public class MemoryDatum
{
    public MemoryDatum()
    {
    }

    public MemoryDatum(object? value)
    {
        Value = value;
    }
        
    public object? Value { get; set; }
}