using Elsa.Workflows.Models;

namespace Elsa.Scheduling;

public class ScheduleExistingWorkflowInstanceRequest
{
    /// <summary>
    /// The ID of the workflow instance.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!;

    /// <summary>
    /// The ID of the bookmark that triggered the workflow instance, if any.
    /// </summary>
    public string? BookmarkId { get; set; }
    
    /// <summary>
    /// The handle of the activity to schedule, if any.
    /// </summary>
    public ActivityHandle? ActivityHandle { get; set; }
    
    /// <summary>
    /// Any additional properties to associate with the workflow instance.
    /// </summary>
    public IDictionary<string, object>? Properties { get; set; }
    
    /// <summary>
    /// The input to the workflow instance, if any.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }
}