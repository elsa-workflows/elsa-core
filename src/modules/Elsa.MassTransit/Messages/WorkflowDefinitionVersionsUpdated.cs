namespace Elsa.MassTransit.Messages;

/// <summary>
/// Represents a distributed message that multiple workflow definition versions have been deleted.
/// </summary>
public class WorkflowDefinitionVersionsUpdated(IDictionary<string, bool> definitionsAsActivity)
{
    /// <summary>
    /// A dictionary of the definition version ID combined with if the workflow is marked as usable as activity. 
    /// </summary>
    public IDictionary<string, bool> DefinitionsAsActivity { get; set; } = definitionsAsActivity;
}