namespace Elsa.Bpmn.Interchange.Exceptions;

/// <summary>
/// Thrown when <see cref="Services.BpmnInterchangeDocumentService.ImportDocumentAsync"/> is asked to edit the BPMN
/// document of a workflow definition that no longer exists.
/// </summary>
/// <remarks>
/// <see cref="Services.BpmnInterchangeDocumentService.ImportDocumentAsync"/> edits an <em>existing</em> definition by
/// passing it as the shared import logic's <c>preserveMetadataFrom</c> parameter; a missing preservation source must
/// never fall through to the whole-definition import path <c>preserveMetadataFrom: null</c> takes; that path would
/// silently create a definition under the requested id with reset metadata instead of reporting that the PUT's
/// target disappeared. If the definition is deleted between the PUT endpoint's own existence/ETag check and this
/// method's lookup, this is what makes that race surface as a refusal rather than a silent, metadata-resetting
/// import.
/// </remarks>
public class BpmnDefinitionNotFoundException(string message) : Exception(message);
