using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Models;

public class Output : Argument
{
    public Output() : base(new Literal())
    {
    }

    public Output(MemoryBlockReference memoryBlockReference) : base(memoryBlockReference)
    {
    }
    
    public Output(Func<MemoryBlockReference> memoryBlockReference) : base(memoryBlockReference)
    {
    }
}

public class Output<T> : Output
{
    public Output()
    {
    }
    
    public Output(MemoryBlockReference memoryBlockReference) : base(memoryBlockReference)
    {
    }
    
    public Output(Func<MemoryBlockReference> memoryBlockReference) : base(memoryBlockReference)
    {
    }
}