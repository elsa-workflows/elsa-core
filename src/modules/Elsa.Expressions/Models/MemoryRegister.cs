namespace Elsa.Expressions.Models;

/// <summary>
/// Represents a register of memory. 
/// </summary>
public class MemoryRegister
{
    public MemoryRegister(MemoryRegister? parent = default, IDictionary<string, MemoryDatum>? locations = default)
    {
        Parent = parent;
        Memory = locations ?? new Dictionary<string, MemoryDatum>();
    }

    public MemoryRegister? Parent { get; }
    public IDictionary<string, MemoryDatum> Memory { get; }

    public bool TryGetMemoryDatum(string id, out MemoryDatum datum)
    {
        datum = null!;
        
        if (Memory.TryGetValue(id, out datum!))
            return true;

        return Parent?.TryGetMemoryDatum(id, out datum) == true;
    }

    public void Declare(IEnumerable<MemoryDatumReference> references)
    {
        foreach (var reference in references)
            Declare(reference);
    }

    public MemoryDatum Declare(MemoryDatumReference reference)
    {
        var datum = reference.Declare();
        Memory[reference.Id] = datum;
        return datum;
    }
}