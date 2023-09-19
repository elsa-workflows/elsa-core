using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Represents options for starting a workflow host.
/// </summary>
/// <param name="InstanceId">An optional workflow instance ID. If not specified, a new ID will be generated.</param>
/// <param name="CorrelationId">An optional correlation ID.</param>
/// <param name="Input">Optional input to pass to the workflow instance.</param>
/// <param name="TriggerActivityId">The ID of the activity that triggered the workflow instance.</param>
/// <param name="CancellationTokens">Cancellation tokens that can be used to cancel the workflow instance without cancelling system-level operations.</param>
public record StartWorkflowHostOptions(
    string? InstanceId = default,
    string? CorrelationId = default,
    IDictionary<string, object>? Input = default,
    string? TriggerActivityId = default,
    CancellationTokens CancellationTokens = default);