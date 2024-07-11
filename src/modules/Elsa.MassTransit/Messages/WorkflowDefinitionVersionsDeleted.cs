namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a distributed message that multiple workflow definition versions have been deleted.
/// </summary>
public class WorkflowDefinitionVersionsDeleted(IEnumerable<string> ids)
{
    /// <summary>
    /// The IDs of the deleted workflow definition versions.
    /// </summary>
    public IEnumerable<string> Ids { get; set; } = ids;
}