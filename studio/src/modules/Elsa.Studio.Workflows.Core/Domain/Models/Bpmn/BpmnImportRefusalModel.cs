namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// Why an import of the analyzed document would be refused, as <c>bpmn/analyze</c> reports it. Mirrors
/// <c>Elsa.Bpmn.Interchange.Endpoints.Bpmn.BpmnImportRefusalModel</c> in elsa-core.
/// </summary>
public class BpmnImportRefusalModel
{
    /// <summary>The error code the import would answer with, such as <c>bpmn.import.binding-invalid</c>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Why the import would be refused.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>The ids of the elements and sequence flows the refusal is about; empty when it names none.</summary>
    public IReadOnlyCollection<string> ElementIds { get; set; } = [];
}
