namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// The payload every execution log entry projected from a BPMN diagnostic carries. Mirrors elsa-core's
/// <c>Elsa.Bpmn.Hosting.BpmnDiagnosticLogPayload</c> -- the source of truth for this shape -- field for field, since
/// that record is exactly what arrives, camelCase, as <see cref="Elsa.Api.Client.Resources.WorkflowInstances.Models.WorkflowExecutionLogRecord.Payload"/>
/// for an entry whose <c>Source</c> is <see cref="BpmnDiagnosticEventNames.Source"/>.
/// </summary>
/// <param name="DiagnosticId">The diagnostic's id in the interpreter's own pinned id stream (<c>diag:N</c>).</param>
/// <param name="ElementId">The BPMN element the diagnostic is about, or <c>null</c> when it names none.</param>
/// <param name="FlowId">The BPMN sequence flow the diagnostic is about, or <c>null</c> when it names none.</param>
/// <param name="TokenId">The token the diagnostic is about, or <c>null</c> when it names none.</param>
/// <param name="Kind">The diagnostic kind's enum member name; see <see cref="BpmnDiagnosticEventNames"/>.</param>
/// <param name="Details">Free-form key/value details the interpreter attached, carried verbatim.</param>
public sealed record BpmnDiagnosticLogPayload(
    string DiagnosticId,
    string? ElementId,
    string? FlowId,
    string? TokenId,
    string Kind,
    IReadOnlyDictionary<string, string>? Details);
