namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a distributed message that multiple workflow definitions have been deleted.
/// </summary>
public class WorkflowDefinitionsDeleted(IEnumerable<string> ids)
{
    public IEnumerable<string> Ids { get; set; } = ids;
}