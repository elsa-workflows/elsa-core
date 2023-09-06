using Elsa.Common.Models;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Represents options for starting a workflow.
/// </summary>
/// <param name="CorrelationId"></param>
/// <param name="Input"></param>
/// <param name="VersionOptions"></param>
/// <param name="TriggerActivityId"></param>
/// <param name="InstanceId"></param>
public record StartWorkflowRuntimeOptions(
    string? CorrelationId = default, 
    IDictionary<string, object>? Input = default, 
    VersionOptions VersionOptions = default, 
    string? TriggerActivityId = default, 
    string? InstanceId = default);