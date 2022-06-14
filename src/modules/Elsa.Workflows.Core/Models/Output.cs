using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Models;

public class Output : Argument
{
    public Output() : base(new Literal())
    {
    }

    public Output(MemoryBlockReference locationBlockReference) : base(locationBlockReference)
    {
    }

    //public ICollection<MemoryReference> Targets { get; } = new List<MemoryReference>();
}

public class Output<T> : Output
{
    public Output()
    {
    }
    
    public Output(MemoryBlockReference locationBlockReference) : base(locationBlockReference)
    {
    }
}