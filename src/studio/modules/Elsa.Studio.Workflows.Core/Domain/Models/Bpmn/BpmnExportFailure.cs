namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// Why a BPMN export could not be produced, carrying both the classified <see cref="Reason"/> the UI should
/// translate itself, and the server's own message as a fallback for <see cref="BpmnExportFailureReason.Unknown"/>.
/// </summary>
public record BpmnExportFailure(BpmnExportFailureReason Reason, string Message);
