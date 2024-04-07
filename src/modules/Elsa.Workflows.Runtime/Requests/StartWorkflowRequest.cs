using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Abstractions;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Requests;

public class StartWorkflowRequest : ExecuteWorkflowRequestBase, IExecuteWorkflowRequest
{
    public string? TriggerActivityId { get; set; }
    public string? ParentWorkflowInstanceId { get; set; }
    string? IExecuteWorkflowRequest.BookmarkId { get; set; }
    ActivityHandle? IExecuteWorkflowRequest.ActivityHandle { get; set; }
}