using Elsa.Common.Models;

namespace Elsa.MassTransit.Messages;

public record DispatchWorkflowDefinition(string DefinitionId, VersionOptions VersionOptions, IDictionary<string, object>? Input, string? CorrelationId, string? InstanceId, string? TriggerActivityId);