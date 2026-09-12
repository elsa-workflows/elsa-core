using JetBrains.Annotations;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>
/// The workflow definition an import produced, plus what the read cost. Shared between <c>Import</c> and the
/// document <c>Put</c> endpoint, since <c>Put</c> runs the same import path and reports the same shape for it.
/// </summary>
[PublicAPI]
public sealed class BpmnImportResponse
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
