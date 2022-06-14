namespace Elsa.Expressions.Models;

/// <summary>
/// Represents a register of memory. 
/// </summary>
public class MemoryRegister
{
    public MemoryRegister(MemoryRegister? parent = default, IDictionary<string, MemoryBlock>? locations = default)
    {
        Parent = parent;
        Blocks = locations ?? new Dictionary<string, MemoryBlock>();
    }

    public MemoryRegister? Parent { get; }
    public IDictionary<string, MemoryBlock> Blocks { get; }

    public bool TryGetBlock(string id, out MemoryBlock datum)
    {
        datum = null!;
        
        if (Blocks.TryGetValue(id, out datum!))
            return true;

        return Parent?.TryGetBlock(id, out datum) == true;
    }

    public void Declare(IEnumerable<MemoryBlockReference> references)
    {
        foreach (var reference in references)
            Declare(reference);
    }

    public MemoryBlock Declare(MemoryBlockReference blockReference)
    {
        var datum = blockReference.Declare();
        Blocks[blockReference.Id] = datum;
        return datum;
    }
}