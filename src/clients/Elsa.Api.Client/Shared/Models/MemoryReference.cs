namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents a reference to a memory block.
/// </summary>
public class MemoryReference
{
    /// <summary>
    /// Gets or sets the ID of the memory block being referenced.
    /// </summary>
    public string Id { get; set; } = default!;
}