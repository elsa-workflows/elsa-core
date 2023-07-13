namespace Elsa.Expressions.Models;

/// <summary>
/// A memory block reference type that is used for reading memory blocks only.
/// </summary>
public class ReadReference : MemoryBlockReference
{
    /// <inheritdoc />
    public ReadReference(string id) : base(id)
    {
    }
    
    /// <inheritdoc />
    public override MemoryBlock Declare()
    {
        return new MemoryBlock();
    }
}