using Elsa.Models;

namespace Elsa.Services.Models
{
    /// <summary>
    /// Represents a workflow that was either found in the database, or instantiated on the fly for initial execution.
    /// In case the workflow instance ID was found in the DB, it will not yet have been loaded (which usually isn't necessary).
    /// Otherwise, if the workflow instance was instantiated on the fly for new workflow runs, the workflow instance is provided to give the caller a chance to persist it into the database.
    /// The reason for treating existing workflow instances and new ones differently is to prevent new workflow instances from being persisted without the caller actually invoking them (with input).
    /// This causes un-started (Idle) workflows with no bound input, causing them to fail when a new application instance starts.
    /// </summary>
    public record CollectedWorkflow(string WorkflowInstanceId, WorkflowInstance? WorkflowInstance, string? ActivityId);
}