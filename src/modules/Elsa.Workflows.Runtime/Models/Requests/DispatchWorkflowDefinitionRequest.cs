using Elsa.Common.Models;

namespace Elsa.Workflows.Runtime.Models.Requests;

public record DispatchWorkflowDefinitionRequest(string DefinitionId, VersionOptions VersionOptions, IDictionary<string, object>? Input = default, string? CorrelationId = default, string? InstanceId = default);