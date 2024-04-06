using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Parameters;

public class StartWorkflowParams : ExecuteWorkflowParamsCommonBase, IExecuteWorkflowParams
{
    public string? TriggerActivityId { get; set; }
    public string? ParentWorkflowInstanceId { get; set; }
    string? IExecuteWorkflowParams.BookmarkId { get; set; }
    ActivityHandle? IExecuteWorkflowParams.ActivityHandle { get; set; }
}