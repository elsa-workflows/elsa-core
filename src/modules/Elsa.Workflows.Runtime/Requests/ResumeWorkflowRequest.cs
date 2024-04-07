using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Abstractions;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Requests;

public class ResumeWorkflowRequest : ExecuteWorkflowRequestBase, IExecuteWorkflowRequest
{
    public string? BookmarkId { get; set; }
    public ActivityHandle? ActivityHandle { get; set; }
    string? IExecuteWorkflowRequest.CorrelationId { get; set; }
    string? IExecuteWorkflowRequest.TriggerActivityId { get; set; }
    string? IExecuteWorkflowRequest.ParentWorkflowInstanceId { get; set; }
}