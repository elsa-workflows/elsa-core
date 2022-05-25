using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Models;

public class Output : Argument
{
    public Output() : base(new Literal())
    {
    }

    public Output(RegisterLocationReference locationReference) : this()
    {
        Targets.Add(locationReference);
    }

    public ICollection<RegisterLocationReference> Targets { get; } = new List<RegisterLocationReference>();
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