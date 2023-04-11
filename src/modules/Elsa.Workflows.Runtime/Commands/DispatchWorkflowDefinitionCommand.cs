using Elsa.Common.Models;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;

namespace Elsa.Workflows.Runtime.Commands;

/// <summary>
/// Dispatches a workflow definition.
/// </summary>
public record DispatchWorkflowDefinitionCommand(
    string DefinitionId, 
    VersionOptions VersionOptions, 
    IDictionary<string, object>? Input = default, 
    string? CorrelationId = default,
    string? InstanceId = default,
    string? TriggerActivityId = default) : ICommand<Unit>;

// ReSharper disable once UnusedType.Global