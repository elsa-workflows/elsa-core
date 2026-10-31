namespace Elsa.Studio.Bpmn.Designers;

/// <summary>What a designer's <c>load(xml)</c> returned.</summary>
/// <param name="Imported">Whether the designer shows the document. When false, it keeps the document it had.</param>
/// <param name="Error">Why the document could not be shown, when <paramref name="Imported"/> is false.</param>
/// <param name="Warnings">What the designer cannot represent or will change; see <see cref="BpmnSaveGate"/>.</param>
public sealed record BpmnDesignerLoadResult(bool Imported, string? Error, IReadOnlyList<BpmnDesignerLoadWarning> Warnings);
