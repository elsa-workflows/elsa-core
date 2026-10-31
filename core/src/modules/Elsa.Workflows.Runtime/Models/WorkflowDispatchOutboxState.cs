namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// Workflow-state marker proving that outbox items belong to a committed workflow state.
/// </summary>
public class WorkflowDispatchOutboxState
{
    /// <summary>
    /// Outbox item IDs committed with the workflow state.
    /// </summary>
    public ICollection<string> ItemIds { get; set; } = new List<string>();
}
