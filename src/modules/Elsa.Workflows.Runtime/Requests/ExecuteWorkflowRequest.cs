using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Requests;

/// <summary>
/// Provides parameters for executing a workflow.
/// </summary>
public class ExecuteWorkflowRequest : IExecuteWorkflowRequest
{
    public string DefinitionVersionId { get; set; }
    public string? CorrelationId { get; set; }
    public string? BookmarkId { get; set; }
    public ActivityHandle? ActivityHandle { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public string? TriggerActivityId { get; set; }
    public string? ParentWorkflowInstanceId { get; set; }
    public string? InstanceId { get; set; }
    public bool? IsExistingInstance { get; set; }
}