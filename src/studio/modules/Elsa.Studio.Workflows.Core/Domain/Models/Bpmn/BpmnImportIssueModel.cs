namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// One element-scoped finding from a BPMN read, as reported by the <c>bpmn/analyze</c> and <c>bpmn/import</c>
/// endpoints. Mirrors <c>Elsa.Bpmn.Interchange.Endpoints.Bpmn.BpmnImportIssueModel</c> in elsa-core.
/// </summary>
public class BpmnImportIssueModel
{
    /// <summary>
    /// How much of the element's authored meaning survived the read: <c>Info</c>, <c>Degraded</c> or <c>Dropped</c>.
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// A sentence naming the element, what the document said, and what the reader did about it.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The BPMN id the finding is about, when it is about one element.
    /// </summary>
    public string? ElementId { get; set; }

    /// <summary>
    /// The process the finding occurred in, when it is scoped to one.
    /// </summary>
    public string? ProcessId { get; set; }
}
