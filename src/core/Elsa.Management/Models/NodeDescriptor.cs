namespace Elsa.Management.Models;

public abstract class NodeDescriptor
{
    public string NodeType { get; init; } = default!;
    public string Category { get; init; } = default!;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public ICollection<InputDescriptor> InputProperties { get; init; } = new List<InputDescriptor>();
    public ICollection<OutputDescriptor> OutputProperties { get; init; } = new List<OutputDescriptor>();
}