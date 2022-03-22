using Elsa.Persistence.Models;

namespace Elsa.Runtime.Models;

public record ExecuteWorkflowDefinitionRequest(string DefinitionId, VersionOptions VersionOptions, IReadOnlyDictionary<string, object>? Input = default);