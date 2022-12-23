namespace Elsa.Expressions.Models;

/// <summary>
/// Represents a piece of memory within a memory register
/// </summary>
public class MemoryBlock
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public MemoryBlock()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public MemoryBlock(object? value, object? metadata = default)
    {
        Value = value;
        Metadata = metadata;
    }
        
    /// <summary>
    /// The value stored in this block.
    /// </summary>
    public object? Value { get; set; }
    
    /// <summary>
    /// Optional metadata about this block.
    /// </summary>
    public object? Metadata { get; set; }
}