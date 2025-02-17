using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Messages;

/// <summary>
/// Represents a response from running a workflow instance.
/// </summary>
public record RunWorkflowInstanceResponse
{
    /// <summary>
    /// The ID of the workflow instance.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!; 
    
    /// <summary>
    /// The status of the workflow instance.
    /// </summary>
    public WorkflowStatus Status { get; set; }

    /// <summary>
    /// The sub-status of the workflow instance.
    /// </summary>
    public WorkflowSubStatus SubStatus { get; set; }

    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    /// <summary>
    /// Any incidents that occurred during the execution of the workflow instance.
    /// </summary>
    public ICollection<ActivityIncident> Incidents { get; set; } = new List<ActivityIncident>();
}