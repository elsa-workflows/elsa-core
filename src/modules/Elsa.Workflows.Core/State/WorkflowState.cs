using System.Text.Json.Serialization;
using Elsa.Common.Entities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.Workflows.Core.State;

/// <summary>
/// Represents the current state of a workflow. 
/// </summary>
public class WorkflowState : Entity
{
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
    /// The serialized workflow state, if any. 
    /// </summary>
    public WorkflowFaultState? Fault { get; set; }

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
    [JsonConverter(typeof(PropertyBagConverter))]
    public PropertyBag Properties { get; set; } = new();
}