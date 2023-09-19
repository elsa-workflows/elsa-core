using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Options for triggering workflows.
/// </summary>
public class TriggerWorkflowsOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerWorkflowsOptions"/> class.
    /// </summary>
    public TriggerWorkflowsOptions(
        string? correlationId = default, 
        string? workflowInstanceId = default, 
        string? activityInstanceId = default, 
        IDictionary<string, object>? input = default,
        CancellationTokens cancellationTokens = default)
    {
        CorrelationId = correlationId;
        WorkflowInstanceId = workflowInstanceId;
        ActivityInstanceId = activityInstanceId;
        Input = input;
        CancellationTokens = cancellationTokens;
    }

    public string? CorrelationId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public CancellationTokens CancellationTokens { get; set; }
}