using JetBrains.Annotations;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Xml.Put;

/// <summary>The workflow definition an XML save produced, what the read cost, and the foreign content the save dropped.</summary>
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

    /// <summary>
    /// The foreign content the previously stored document had that this one does not: elements and attributes in a
    /// namespace that is neither BPMN's own nor Elsa's. The save succeeded regardless; empty when nothing was dropped.
    /// </summary>
    public IReadOnlyList<BpmnForeignContentModel> DroppedForeignContent { get; init; } = [];
}
