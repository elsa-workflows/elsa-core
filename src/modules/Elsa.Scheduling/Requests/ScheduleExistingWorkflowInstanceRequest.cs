using Elsa.Workflows.Models;

namespace Elsa.Scheduling;

public class ScheduleExistingWorkflowInstanceRequest
{
    /// The ID of the workflow instance.
    public string WorkflowInstanceId { get; set; } = default!;

    /// The ID of the bookmark that triggered the workflow instance, if any.
    public string? BookmarkId { get; set; }
    
    /// The handle of the activity to schedule, if any.
    public ActivityHandle? ActivityHandle { get; set; }
    
    /// Any additional properties to associate with the workflow instance.
    public IDictionary<string, object>? Properties { get; set; }
    
    /// The input to the workflow instance, if any.
    public IDictionary<string, object>? Input { get; set; }
}