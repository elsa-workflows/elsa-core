using Elsa.Persistence.Models;

namespace Elsa.Runtime.Models;

public record InvokeWorkflowDefinitionRequest(string DefinitionId, VersionOptions VersionOptions, IDictionary<string, object>? Input = default, string? CorrelationId = default);