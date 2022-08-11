using Elsa.Persistence.Common.Models;

namespace Elsa.Workflows.Runtime.Models;

public record DispatchWorkflowDefinitionRequest(string DefinitionId, VersionOptions VersionOptions, IDictionary<string, object>? Input = default, string? CorrelationId = default);