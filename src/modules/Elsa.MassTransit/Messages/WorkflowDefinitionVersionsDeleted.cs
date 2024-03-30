namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a distributed message that multiple workflow definition versions have been deleted.
/// </summary>
public class WorkflowDefinitionVersionsDeleted(IEnumerable<string> ids)
{
    public IEnumerable<string> Ids { get; set; } = ids;
}