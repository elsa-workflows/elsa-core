namespace Elsa.Runtime.Models;

public record ExecuteWorkflowDefinitionRequest(string DefinitionId, int Version, IReadOnlyDictionary<string, object>? Input = default);