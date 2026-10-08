using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Messages;

namespace Elsa.Workflows.Runtime;

public record StartWorkflowResponse
{
    /// <summary>
    /// Indicates whether the workflow instance can be started.
    /// </summary>
    public bool CannotStart { get; set; }
    
    /// <summary>
    /// The ID of the workflow instance.
    /// </summary>
    public string? WorkflowInstanceId { get; set; } 
    
    /// <summary>
    /// The status of the workflow instance.
    /// </summary>
    public WorkflowStatus? Status { get; set; }

    /// <summary>
    /// The sub-status of the workflow instance.
    /// </summary>
    public WorkflowSubStatus? SubStatus { get; set; }
    
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    /// <summary>
    /// Any incidents that occurred during the execution of the workflow instance.
    /// </summary>
    public ICollection<ActivityIncident> Incidents { get; set; } = new List<ActivityIncident>();
    
    public RunWorkflowInstanceResponse ToRunWorkflowInstanceResponse() => new()
    {
        WorkflowInstanceId = WorkflowInstanceId!,
        Status = Status!.Value,
        SubStatus = SubStatus!.Value,
        Incidents = Incidents
    };
}