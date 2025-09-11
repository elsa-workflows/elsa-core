using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime;

public class ResumeBookmarkRequest
{
    public string WorkflowInstanceId { get; set; } = null!;

    /// The ID of the bookmark that triggered the workflow instance, if any.
    public string BookmarkId { get; set; } = null!;
    
    /// The handle of the activity to schedule, if any.
    [Obsolete("Use ActivityInstanceId instead")]
    public ActivityHandle? ActivityHandle { get; set; }
    
    /// <summary>
    /// The ID of the activity instance to resume, if any.
    /// </summary>
    public string? ActivityInstanceId { get; set; }
    
    /// Any additional properties to associate with the workflow instance.
    public IDictionary<string, object>? Properties { get; set; }
    
    /// The input to the workflow instance, if any.
    public IDictionary<string, object>? Input { get; set; }
}