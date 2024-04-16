using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Messages;

/// <summary>
/// A request to run a workflow instance.
/// </summary>
[UsedImplicitly]
public class RunWorkflowInstanceRequest
{
    /// The ID of the activity that triggered the workflow instance, if any.
    public string? TriggerActivityId { get; set; }
    
    /// The ID of the bookmark that triggered the workflow instance, if any.
    public string? BookmarkId { get; set; }
    
    /// The handle of the activity to schedule, if any.
    public ActivityHandle? ActivityHandle { get; set; }
    
    /// Any additional properties to associate with the workflow instance.
    public IDictionary<string, object>? Properties { get; set; }
    
    /// The input to the workflow instance, if any.
    public IDictionary<string, object>? Input { get; set; }
}