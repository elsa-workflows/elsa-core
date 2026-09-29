namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// What a document contains and how much of it survives a read, as reported by the <c>bpmn/analyze</c> and
/// <c>bpmn/import</c> endpoints. Mirrors <c>Elsa.Bpmn.Interchange.Endpoints.Bpmn.BpmnImportAnalysisModel</c> in
/// elsa-core.
/// </summary>
public class BpmnImportAnalysisModel
{
    /// <summary>
    /// The <c>id</c> of every <c>&lt;process&gt;</c> in the document, in document order.
    /// </summary>
    public IReadOnlyCollection<string> ProcessIds { get; set; } = [];

    /// <summary>
    /// How many of each BPMN local name were encountered, across all containers.
    /// </summary>
    public IReadOnlyDictionary<string, int> ElementCounts { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// Every finding, in the order the reader produced it.
    /// </summary>
    public IReadOnlyCollection<BpmnImportIssueModel> Issues { get; set; } = [];
}
