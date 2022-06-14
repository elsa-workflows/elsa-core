using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Models;

public abstract class Argument
{
    protected Argument(){}
    
    protected Argument(MemoryBlockReference memoryBlockReference /*, Func<object?, object?>? valueConverter = default*/)
    {
        MemoryBlockReference = memoryBlockReference;
        //ValueConverter = valueConverter;
    }

    public MemoryBlockReference MemoryBlockReference { get; set; } = default!;
    //public Func<object?, object?>? ValueConverter { get; set; }
}