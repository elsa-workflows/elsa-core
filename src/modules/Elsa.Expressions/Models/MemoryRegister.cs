namespace Elsa.Expressions.Models;

/// <summary>
/// Represents a register of memory. 
/// </summary>
public class MemoryRegister
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public MemoryRegister(IDictionary<string, MemoryBlock>? blocks = default)
    {
        Blocks = blocks ?? new Dictionary<string, MemoryBlock>();
    }
    
    /// <summary>
    /// The memory blocks declared in this register.
    /// </summary>
    public IDictionary<string, MemoryBlock> Blocks { get; }

    /// <summary>
    /// Returns true if the specified memory block is declared in this register.
    /// </summary>
    public bool IsDeclared(MemoryBlockReference reference) => HasBlock(reference.Id);

    /// <summary>
    /// Returns true if the specified memory block is declared in this register.
    /// </summary>
    public bool HasBlock(string id) => Blocks.ContainsKey(id);
    
    /// <summary>
    /// Returns the memory block with the specified ID.
    /// </summary>
    public bool TryGetBlock(string id, out MemoryBlock block)
    {
        block = null!;
        return Blocks.TryGetValue(id, out block!);
    }

    /// <summary>
    /// Declares the memory for the specified memory block references. 
    /// </summary>
    public void Declare(IEnumerable<MemoryBlockReference> references)
    {
        foreach (var reference in references)
            Declare(reference);
    }

    /// <summary>
    /// Declares the memory for the specified memory block reference.
    /// </summary>
    public MemoryBlock Declare(MemoryBlockReference blockReference)
    {
        if(Blocks.TryGetValue(blockReference.Id, out var block))
            return block;

        block = blockReference.Declare();
        Blocks[blockReference.Id] = block;
        return block;
    }
}