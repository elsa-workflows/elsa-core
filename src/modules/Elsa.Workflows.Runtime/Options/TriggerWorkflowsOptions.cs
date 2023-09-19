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
        CancellationToken applicationCancellationToken = default, 
        CancellationToken systemCancellationToken = default)
    {
        CorrelationId = correlationId;
        WorkflowInstanceId = workflowInstanceId;
        ActivityInstanceId = activityInstanceId;
        Input = input;
        ApplicationCancellationToken = applicationCancellationToken;
        SystemCancellationToken = systemCancellationToken;
    }

    public string? CorrelationId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public CancellationToken ApplicationCancellationToken { get; set; }
    public CancellationToken SystemCancellationToken { get; set; }
}