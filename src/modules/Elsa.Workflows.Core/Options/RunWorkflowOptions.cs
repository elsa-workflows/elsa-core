using Elsa.Workflows.Models;

namespace Elsa.Workflows.Options;

/// <summary>
/// Provides options for running a workflow.
/// </summary>
public class RunWorkflowOptions
{
    public string? WorkflowInstanceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? BookmarkId { get; set; }
    public string? ActivityId { get; set; }
    public string? ActivityNodeId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public string? ActivityHash { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public string? TriggerActivityId { get; set; }
    public CancellationTokens CancellationTokens { get; set; }
    /// <summary>
    /// The user/service who triggered this workflow.
    /// When the workflow is triggered from the Studio, this value will be the user Identity name (who logged in).
    /// </summary>
    public string? Requester { get; set; }
}