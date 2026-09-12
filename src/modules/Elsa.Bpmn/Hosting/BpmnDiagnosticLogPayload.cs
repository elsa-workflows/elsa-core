namespace Elsa.Bpmn.Hosting;

/// <summary>
/// The payload every execution log entry <see cref="BpmnScopeHost"/> projects from a BPMN diagnostic carries.
/// </summary>
/// <remarks>
/// This shape is a compatibility surface: Studio's instance viewer overlay reads it to key a decision back onto
/// the BPMN element — and, for a decision about a sequence flow, the flow — it belongs to, since under Option A
/// only bound work has an activity id. <see cref="Kind"/> carries the diagnostic kind's own enum member name
/// (see <see cref="BpmnDiagnosticEventNames"/>) rather than its integer value, so a <c>Bpmn.Model</c> upgrade that
/// reorders or extends <c>BpmnDiagnosticKind</c> cannot silently change what a reader keyed on the number would see.
/// </remarks>
/// <param name="DiagnosticId">The diagnostic's id in the interpreter's own pinned id stream (<c>diag:N</c>).</param>
/// <param name="ElementId">The BPMN element the diagnostic is about, or <c>null</c> when it names none.</param>
/// <param name="FlowId">The BPMN sequence flow the diagnostic is about, or <c>null</c> when it names none.</param>
/// <param name="TokenId">The token the diagnostic is about, or <c>null</c> when it names none.</param>
/// <param name="Kind">The diagnostic kind's enum member name.</param>
/// <param name="Details">Free-form key/value details the interpreter attached, carried verbatim.</param>
public sealed record BpmnDiagnosticLogPayload(
    string DiagnosticId,
    string? ElementId,
    string? FlowId,
    string? TokenId,
    string Kind,
    IReadOnlyDictionary<string, string> Details);
