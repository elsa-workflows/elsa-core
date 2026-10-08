using JetBrains.Annotations;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Import;

/// <summary>The workflow definition a BPMN import produced, plus what the read cost.</summary>
[PublicAPI]
public sealed class Response
{
    /// <summary>The persisted version's own id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The workflow definition id, stable across versions.</summary>
    public string DefinitionId { get; init; } = string.Empty;

    /// <summary>The persisted version number.</summary>
    public int Version { get; init; }

    /// <summary>The same analysis <see cref="Analyze.Analyze"/> would have produced for this document.</summary>
    public BpmnImportAnalysisModel Analysis { get; init; } = new();
}
