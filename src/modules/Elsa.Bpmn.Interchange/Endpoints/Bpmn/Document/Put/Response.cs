using JetBrains.Annotations;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document.Put;

/// <summary>
/// The workflow definition draft a document edit produced, plus what the read cost — the same shape
/// <c>Endpoints.Bpmn.Import.Response</c> reports for a fresh import, since this endpoint runs the same import path.
/// </summary>
[PublicAPI]
public sealed class Response
{
    /// <summary>The persisted draft version's own id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The workflow definition id, stable across versions.</summary>
    public string DefinitionId { get; init; } = string.Empty;

    /// <summary>The persisted draft's version number.</summary>
    public int Version { get; init; }

    /// <summary>The Info/Degraded/Dropped findings the write produced, so Studio can show what the edit cost.</summary>
    public BpmnImportAnalysisModel Analysis { get; init; } = new();
}
