namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents an output binding for an activity.
/// </summary>
public class ActivityOutput
{
    /// <summary>
    /// Gets or sets the type of this output.
    /// </summary>
    public string TypeName { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the memory reference of this output.
    /// </summary>
    public MemoryReference MemoryReference { get; set; } = default!;
}