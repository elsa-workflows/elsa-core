using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Models;

public abstract class Argument
{
    protected Argument()
    {
    }

    protected Argument(MemoryBlockReference memoryBlockReference) : this(() => memoryBlockReference)
    {
    }
    
    protected Argument(Func<MemoryBlockReference> memoryBlockReference)
    {
        MemoryBlockReference = memoryBlockReference;
    }

    public Func<MemoryBlockReference> MemoryBlockReference { get; set; } = default!;
}