namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// One thing worth telling the user about a BPMN document, in document order. Mirrors the
/// TypeScript <c>BpmnDiagnostic</c> that <c>loadBpmnDiagram</c> returns.
/// </summary>
/// <param name="Code">The diagnostic code, e.g. <c>missing-source-xml</c> or <c>unbound-work</c>.</param>
/// <param name="Severity">One of <c>info</c>, <c>warning</c> or <c>error</c>.</param>
/// <param name="Message">A human-readable description of the diagnostic.</param>
/// <param name="ElementId">The BPMN element the diagnostic is about, if any.</param>
/// <param name="ScopeId">The BPMN process scope the diagnostic is about, if any.</param>
public record BpmnDiagnostic(string Code, string Severity, string Message, string? ElementId, string? ScopeId);
