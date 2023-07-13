using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents an input value for an activity.
/// </summary>
public class WrappedInput
{
    /// <summary>
    /// Gets or sets the type of this input.
    /// </summary>
    public string TypeName { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the expression of this input.
    /// </summary>
    public IExpression Expression { get; set; } = default!;

    /// <summary>
    /// Gets or sets the memory reference of this input.
    /// </summary>
    public MemoryReference MemoryReference { get; set; } = default!;
}