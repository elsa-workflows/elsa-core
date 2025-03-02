using Elsa.Common.Models;

namespace Elsa.Workflows.Runtime.Parameters;

/// <summary>
/// Represents parameters for starting a workflow.
/// </summary>
[Obsolete("This type is obsolete. Use the new CreateClientAsync methods of IWorkflowRuntime instead.")]
public class StartWorkflowRuntimeParams
{
    public string? CorrelationId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public VersionOptions VersionOptions { get; set; }
    public string? TriggerActivityId { get; set; }
    public string? InstanceId { get; set; }
    public bool TenantAgnostic { get; set; }
    public CancellationToken CancellationToken { get; set; }    
    public string? ParentWorkflowInstanceId { get; set; }
}