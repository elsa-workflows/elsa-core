using Elsa.Common.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Parameters;

/// <summary>
/// Represents parameters for starting a workflow.
/// </summary>
public class StartWorkflowRuntimeParams
{
    public string? CorrelationId { get; set; }

    public IDictionary<string, object>? Input { get; set; }

    public IDictionary<string, object>? Properties { get; set; }

    public VersionOptions VersionOptions { get; set; }

    public string? TriggerActivityId { get; set; }

    public string? InstanceId { get; set; }
    public bool IsExistingInstance { get; set; }

    public CancellationTokens CancellationTokens { get; set; }
    
    public string? ParentWorkflowInstanceId { get; set; }
}