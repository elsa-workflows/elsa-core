using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Parameters;

public class ResumeWorkflowParams : ExecuteWorkflowParamsCommonBase, IExecuteWorkflowParams
{
    public string? BookmarkId { get; set; }
    public ActivityHandle? ActivityHandle { get; set; }
    string? IExecuteWorkflowParams.CorrelationId { get; set; }
    string? IExecuteWorkflowParams.TriggerActivityId { get; set; }
    string? IExecuteWorkflowParams.ParentWorkflowInstanceId { get; set; }
}