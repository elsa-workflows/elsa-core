namespace Elsa.Bpmn.Interchange.Exceptions;

/// <summary>
/// Thrown when <see cref="Services.BpmnInterchangeDocumentService.ImportDocumentAsync"/> loses the
/// compare-and-swap against the caller's <c>If-Match</c>: another writer saved the definition after
/// the PUT's early ETag check (or between that check and this save).
/// </summary>
/// <remarks>
/// Mapped to the same <c>412</c> / <c>bpmn.document.precondition-failed</c> response the PUT already
/// sends when <c>If-Match</c> is stale on arrival, so a lost race and a stale header are one refusal.
/// </remarks>
public class BpmnDocumentPreconditionFailedException(string message) : Exception(message);
