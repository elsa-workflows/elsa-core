using System.Text.Json.Serialization;
using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// A base type for the <see cref="Input{T}"/> type.
/// </summary>
public abstract class Argument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Argument"/> class.
    /// </summary>
    protected Argument()
    {
    }

    /// <inheritdoc />
    protected Argument(MemoryBlockReference memoryBlockReference) : this(() => memoryBlockReference)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Argument"/> class.
    /// </summary>
    /// <param name="memoryBlockReference"></param>
    protected Argument(Func<MemoryBlockReference> memoryBlockReference)
    {
        MemoryBlockReference = memoryBlockReference;
    }

    /// <summary>
    /// Gets or sets the memory block reference.
    /// </summary>
    [JsonIgnore]
    public Func<MemoryBlockReference> MemoryBlockReference { get; set; } = default!;
}