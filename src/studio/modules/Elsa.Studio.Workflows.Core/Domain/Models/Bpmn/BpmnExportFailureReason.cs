namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// Why <c>bpmn/definitions/{definitionId}/export</c> refused to export a workflow definition, classified from the
/// exception message <c>Elsa.Bpmn.Interchange.Services.BpmnInterchangeDocumentService.Export(WorkflowDefinition)</c>
/// raises, so the UI can show its own short, localized wording instead of the raw diagnostic sentence.
/// </summary>
public enum BpmnExportFailureReason
{
    /// <summary>The workflow definition could not be found.</summary>
    NotFound,

    /// <summary>The definition does not currently carry BPMN source: it was never imported from BPMN, or a later save replaced its custom properties.</summary>
    NotImportedFromBpmn,

    /// <summary>The definition has changed since it was imported from BPMN, so the stored source no longer corresponds to it.</summary>
    DefinitionChangedSinceImport,

    /// <summary>The export failed for a reason that does not match either of the two refusals above.</summary>
    Unknown
}
