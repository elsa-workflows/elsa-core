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

    /// <summary>
    /// Why an import of the document would be refused, or <see langword="null"/> when it would not or when the document
    /// declares several processes and none was chosen. Only <c>bpmn/analyze</c> reports it.
    /// </summary>
    public BpmnImportRefusalModel? Refusal { get; set; }

    /// <summary>
    /// The id of every task the document binds to no activity. Such a task is accepted with a placeholder that refuses
    /// to run, so the workflow cannot be published until each one is bound.
    /// </summary>
    public IReadOnlyCollection<string> UnboundElementIds { get; set; } = [];
}
