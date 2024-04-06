namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Options for triggering workflows.
/// </summary>
public class TriggerWorkflowsOptions
{
    public string? CorrelationId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public CancellationToken CancellationToken { get; set; }
}