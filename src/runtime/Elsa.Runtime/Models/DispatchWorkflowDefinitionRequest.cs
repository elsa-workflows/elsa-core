using Elsa.Persistence.Models;

namespace Elsa.Runtime.Models;

public record DispatchWorkflowDefinitionRequest(string DefinitionId, VersionOptions VersionOptions, IDictionary<string, object>? Input = default);