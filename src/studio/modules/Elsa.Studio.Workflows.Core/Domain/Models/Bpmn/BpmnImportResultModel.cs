namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// The workflow definition a BPMN import produced, plus what the read cost. Mirrors
/// <c>Elsa.Bpmn.Interchange.Endpoints.Bpmn.Import.Response</c> in elsa-core.
/// </summary>
public class BpmnImportResultModel
{
    /// <summary>
    /// The persisted version's own id.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The workflow definition id, stable across versions.
    /// </summary>
    public string DefinitionId { get; set; } = string.Empty;

    /// <summary>
    /// The persisted version number.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The same analysis the <c>bpmn/analyze</c> endpoint would have produced for this document.
    /// </summary>
    public BpmnImportAnalysisModel Analysis { get; set; } = new();
}
