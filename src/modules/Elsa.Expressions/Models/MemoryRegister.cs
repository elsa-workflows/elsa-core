namespace Elsa.Expressions.Models;

/// <summary>
/// Represents a register of memory. 
/// </summary>
public class MemoryRegister
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public MemoryRegister(MemoryRegister? parent = default, IDictionary<string, MemoryBlock>? blocks = default)
    {
        Parent = parent;
        Blocks = blocks ?? new Dictionary<string, MemoryBlock>();
    }

    public MemoryRegister? Parent { get; }
    public IDictionary<string, MemoryBlock> Blocks { get; }

    public bool IsDeclared(MemoryBlockReference reference) => HasBlock(reference.Id);
    public bool HasBlock(string id) => Blocks.ContainsKey(id);
    
    public bool TryGetBlock(string id, out MemoryBlock block)
    {
        block = null!;
        
        if (Blocks.TryGetValue(id, out block!))
            return true;

        return Parent?.TryGetBlock(id, out block) == true;
    }

    public void Declare(IEnumerable<MemoryBlockReference> references)
    {
        foreach (var reference in references)
            Declare(reference);
    }

    public MemoryBlock Declare(MemoryBlockReference blockReference)
    {
        if (TryGetBlock(blockReference.Id, out var block))
            return block;

        block = blockReference.Declare();
        Blocks[blockReference.Id] = block;
        return block;
    }
}