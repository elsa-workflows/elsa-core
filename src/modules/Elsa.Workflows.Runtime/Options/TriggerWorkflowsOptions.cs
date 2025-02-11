namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Options for triggering workflows.
/// </summary>
[Obsolete("This type is obsolete. Use the new CreateClientAsync methods of IWorkflowRuntime instead.")]
public class TriggerWorkflowsOptions
{
    public string? CorrelationId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public bool TenantAgnostic { get; set; }
    public CancellationToken CancellationToken { get; set; }
}