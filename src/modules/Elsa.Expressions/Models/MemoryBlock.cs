namespace Elsa.Expressions.Models;

/// <summary>
/// Represents a piece of memory within a memory register
/// </summary>
public class MemoryBlock
{
    public MemoryBlock()
    {
    }

    public MemoryBlock(object? value)
    {
        Value = value;
    }
        
    public object? Value { get; set; }
}