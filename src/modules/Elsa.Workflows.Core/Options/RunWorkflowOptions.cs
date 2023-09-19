using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Options;

/// <summary>
/// Provides options for running a workflow.
/// </summary>
public class RunWorkflowOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunWorkflowOptions"/> class.
    /// </summary>
    public RunWorkflowOptions(
        string? workflowInstanceId = default,
        string? correlationId = default,
        string? bookmarkId = default,
        string? activityId = default,
        string? activityNodeId = default,
        string? activityInstanceId = default,
        string? activityHash = default,
        IDictionary<string, object>? input = default,
        string? triggerActivityId = default,
        CancellationTokens cancellationTokens = default)
    {
        WorkflowInstanceId = workflowInstanceId;
        CorrelationId = correlationId;
        BookmarkId = bookmarkId;
        ActivityId = activityId;
        ActivityNodeId = activityNodeId;
        ActivityInstanceId = activityInstanceId;
        ActivityHash = activityHash;
        Input = input;
        TriggerActivityId = triggerActivityId;
        CancellationTokens = cancellationTokens;
    }

    public string? WorkflowInstanceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? BookmarkId { get; set; }
    public string? ActivityId { get; set; }
    public string? ActivityNodeId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public string? ActivityHash { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public string? TriggerActivityId { get; set; }
    public CancellationTokens CancellationTokens { get; set; }
}