namespace Elsa.Expressions.Models;

/// <summary>
/// Represents a register of memory. 
/// </summary>
public class MemoryRegister
{
    public MemoryRegister(MemoryRegister? parent = default, IDictionary<string, MemoryBlock>? locations = default)
    {
        Parent = parent;
        MemoryBlocks = locations ?? new Dictionary<string, MemoryBlock>();
    }

    public MemoryRegister? Parent { get; }
    public IDictionary<string, MemoryBlock> MemoryBlocks { get; }

    public bool TryGetMemoryDatum(string id, out MemoryBlock datum)
    {
        datum = null!;
        
        if (MemoryBlocks.TryGetValue(id, out datum!))
            return true;

        return Parent?.TryGetMemoryDatum(id, out datum) == true;
    }

    public void Declare(IEnumerable<MemoryReference> references)
    {
        foreach (var reference in references)
            Declare(reference);
    }

    public MemoryBlock Declare(MemoryReference reference)
    {
        var datum = reference.Declare();
        MemoryBlocks[reference.Id] = datum;
        return datum;
    }
}