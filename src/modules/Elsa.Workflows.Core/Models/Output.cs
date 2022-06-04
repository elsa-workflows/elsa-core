using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Models;

public class Output : Argument
{
    public Output() : base(new Literal())
    {
    }

    public Output(MemoryDatumReference locationReference) : this()
    {
        Targets.Add(locationReference);
    }

    public ICollection<MemoryDatumReference> Targets { get; } = new List<MemoryDatumReference>();
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