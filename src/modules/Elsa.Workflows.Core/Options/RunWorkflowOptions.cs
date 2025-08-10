using Elsa.Workflows.Models;
using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Options;

/// <summary>
/// Provides options for running a workflow.
/// </summary>
public class RunWorkflowOptions
{
    public string? WorkflowInstanceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? BookmarkId { get; set; }
    public ActivityHandle? ActivityHandle { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Variables { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public string? TriggerActivityId { get; set; }
    public string? ParentWorkflowInstanceId { get; set; }
    
    /// <summary>
    /// Optionally specifies a custom workflow execution pipeline to use when running the workflow.
    /// If not specified, the default pipeline will be used.
    /// </summary>
    public WorkflowMiddlewareDelegate? Pipeline { get; set; }
}