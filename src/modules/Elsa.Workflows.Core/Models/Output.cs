using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Models;

public class Output : Argument
{
    public Output() : base(new Literal())
    {
    }

    public Output(MemoryReference locationReference) : this()
    {
        Targets.Add(locationReference);
    }

    public ICollection<MemoryReference> Targets { get; } = new List<MemoryReference>();
}

public class Output<T> : Output
{
    public Output()
    {
    }
    
    public Output(Variable<T> locationReference) : base(locationReference)
    {
    }
}