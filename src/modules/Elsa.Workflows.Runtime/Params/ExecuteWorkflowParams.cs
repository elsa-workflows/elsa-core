using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Params;

public class ExecuteWorkflowParams
{
    public string? CorrelationId { get; set; }
    public string? BookmarkId { get; set; }
    public ActivityHandle? ActivityHandle { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public string? TriggerActivityId { get; set; }
    public string? ParentWorkflowInstanceId { get; set; }
}