using Elsa.Common.Models;

namespace Elsa.MassTransit.Messages;

/// <summary>
/// A message to dispatch a workflow definition for execution.
/// </summary>
/// <param name="DefinitionId">The ID of the workflow definition to dispatch.</param>
/// <param name="VersionOptions">The version options to use when dispatching the workflow definition.</param>
/// <param name="Input">Any input to pass to the workflow.</param>
/// <param name="Properties">Any properties to attach to the workflow.</param>
/// <param name="CorrelationId">A correlation ID to associate the workflow with.</param>
/// <param name="InstanceId">The ID to use when creating an instance of the workflow to dispatch.</param>
/// <param name="TriggerActivityId">The ID of the activity that triggered the workflow.</param>
public record DispatchWorkflowDefinition(
    string DefinitionId,
    VersionOptions VersionOptions,
    IDictionary<string, object>? Input,
    IDictionary<string, object>? Properties,
    string? CorrelationId,
    string? InstanceId,
    string? TriggerActivityId);