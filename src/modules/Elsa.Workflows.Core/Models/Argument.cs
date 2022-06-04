using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Models;

public abstract class Argument
{
    protected Argument(){}
    
    protected Argument(MemoryDatumReference locationReference /*, Func<object?, object?>? valueConverter = default*/)
    {
        LocationReference = locationReference;
        //ValueConverter = valueConverter;
    }

    public MemoryDatumReference LocationReference { get; set; } = default!;
    //public Func<object?, object?>? ValueConverter { get; set; }
}