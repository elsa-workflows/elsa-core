using System.ComponentModel.DataAnnotations.Schema;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.State;

/// <summary>
/// Represents the current state of a workflow. 
/// </summary>
public class WorkflowState
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// The workflow definition ID.
    /// </summary>
    public string DefinitionId { get; set; } = null!;

    /// <summary>
    /// The workflow definition version ID.
    /// </summary>
    public string DefinitionVersionId { get; set; } = null!;

    /// <summary>
    /// The workflow definition version.
    /// </summary>
    public int DefinitionVersion { get; set; }

    /// <summary>
    /// The ID of the parent workflow.
    /// </summary>
    public string? ParentWorkflowInstanceId { get; set; }

    /// <summary>
    /// The correlation ID of the workflow, if any.
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the workflow instance.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The status of the workflow.
    /// </summary>
    public WorkflowStatus Status { get; set; }

    /// <summary>
    /// The sub status of the workflow.
    /// </summary>
    public WorkflowSubStatus SubStatus { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the workflow instance is actively executing. 
    /// </summary>
    public bool IsExecuting { get; set; }
    
    /// <summary>
    /// Collected bookmarks.
    /// </summary>
    [NotMapped]
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    /// <summary>
    /// A collection of incidents that may have occurred during execution.
    /// </summary>
    public ICollection<ActivityIncident> Incidents { get; set; } = new List<ActivityIncident>();

    /// <summary>
    /// Gets or sets the value indicating whether the workflow is a system workflow.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// A list of callbacks that activities registered in order to be notified when the activities they scheduled complete. 
    /// </summary>
    public ICollection<CompletionCallbackState> CompletionCallbacks { get; set; } = new List<CompletionCallbackState>();

    /// <summary>
    /// A flattened list of <see cref="ActivityExecutionContextState"/> objects, representing the various active "call stacks" of the workflow.
    /// </summary>
    [NotMapped]
    public ICollection<ActivityExecutionContextState> ActivityExecutionContexts { get; set; } = new List<ActivityExecutionContextState>();

    /// <summary>
    /// A list of scheduled activities.
    /// </summary>
    public ICollection<ActivityWorkItemState> ScheduledActivities { get; set; } = new List<ActivityWorkItemState>();

    /// <summary>
    /// The current execution log sequence number.
    /// </summary>
    public long ExecutionLogSequence { get; set; }

    /// <summary>
    /// A dictionary of inputs sent to the workflow.
    /// </summary>
    public IDictionary<string, object> Input { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// A dictionary of outputs produced by the workflow.
    /// </summary>
    public IDictionary<string, object> Output { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// A global property bag that contains properties set by application code and/or activities.
    /// </summary>
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// The created time of the workflow.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The last updated time of the workflow.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// The finished time of the workflow.
    /// </summary>
    public DateTimeOffset? FinishedAt { get; set; }
}