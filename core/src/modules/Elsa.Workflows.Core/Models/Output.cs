using Elsa.Expressions.Models;

namespace Elsa.Workflows.Models;

public class Output : Argument
{
    public Output() : base(new MemoryBlockReference())
    {
    }

    public Output(MemoryBlockReference memoryBlockReference) : base(memoryBlockReference)
    {
    }
    
    public Output(Func<MemoryBlockReference> memoryBlockReference) : base(memoryBlockReference)
    {
    }

    /// <summary>
    /// Gets or sets the optional converter applied to the value delivered by this binding.
    /// </summary>
    public OutputConverterConfiguration? Converter { get; set; }
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
