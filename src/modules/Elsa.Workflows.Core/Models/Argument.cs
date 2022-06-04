using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Models;

public abstract class Argument
{
    protected Argument(){}
    
    protected Argument(MemoryReference memoryReference /*, Func<object?, object?>? valueConverter = default*/)
    {
        MemoryReference = memoryReference;
        //ValueConverter = valueConverter;
    }

    public MemoryReference MemoryReference { get; set; } = default!;
    //public Func<object?, object?>? ValueConverter { get; set; }
}