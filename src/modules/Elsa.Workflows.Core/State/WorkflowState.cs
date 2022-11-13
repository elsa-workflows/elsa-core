using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.State;

/// <summary>
/// Represents the current state of a workflow. 
/// </summary>
public class WorkflowState
{
    /// <summary>
    /// A unique workflow instance ID.
    /// </summary>
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// The workflow definition ID.
    /// </summary>
    public string DefinitionId { get; set; } = default!;
    
    /// <summary>
    /// The workflow definition version.
    /// </summary>
    public int DefinitionVersion { get; set; } = default!;

    /// <summary>
    /// The correlation ID of the workflow, if any.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The status of the workflow.
    /// </summary>
    public WorkflowStatus Status { get; set; }
    
    /// <summary>
    /// The sub status of the workflow.
    /// </summary>
    public WorkflowSubStatus SubStatus { get; set; }

    /// <summary>
    /// Collected bookmarks.
    /// </summary>
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
    
    /// <summary>
    /// A dictionary of activity output.
    /// </summary>
    public IDictionary<string, IDictionary<string, object>> ActivityOutput { get; set; } = new Dictionary<string, IDictionary<string, object>>();
    
    /// <summary>
    /// A list of callbacks that activities registered in order to be notified when the activities they scheduled complete. 
    /// </summary>
    public ICollection<CompletionCallbackState> CompletionCallbacks { get; set; } = new List<CompletionCallbackState>();
    
    /// <summary>
    /// A flattened list of <see cref="ActivityExecutionContextState"/> objects, representing the various active "call stacks" of the workflow.
    /// </summary>
    public ICollection<ActivityExecutionContextState> ActivityExecutionContexts { get; set; } = new List<ActivityExecutionContextState>();
    
    /// <summary>
    /// A global property bag that contains properties set by application code and/or activities.
    /// </summary>
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// A list of variables that can be persisted.
    /// </summary>
    public ICollection<PersistentVariableState> PersistentVariables { get; set; } = new List<PersistentVariableState>();
}