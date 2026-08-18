namespace Elsa.Bpmn.Interchange.Exceptions;

/// <summary>
/// Thrown when a workflow definition cannot be exported as BPMN 2.0 XML because the source this deployment would
/// need to write back out is missing or no longer trustworthy.
/// </summary>
/// <remarks>
/// See <see cref="Services.BpmnInterchangeDocumentService"/>'s remarks for the two distinct situations this covers —
/// a definition never imported from BPMN (or one whose source a later save dropped), and a definition that has
/// changed since it was imported — and why each gets its own message rather than one generic refusal.
/// </remarks>
public class BpmnExportUnavailableException(string message) : Exception(message);
