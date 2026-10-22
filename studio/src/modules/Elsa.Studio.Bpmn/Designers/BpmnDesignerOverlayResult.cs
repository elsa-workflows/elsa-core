namespace Elsa.Studio.Bpmn.Designers;

/// <summary>What a designer's <c>applyOverlays</c> or <c>clearOverlays</c> returned.</summary>
/// <param name="Applied">The ids of the elements that now show an overlay.</param>
/// <param name="UnknownElementIds">The ids the stats named that the document has no element for.</param>
public sealed record BpmnDesignerOverlayResult(IReadOnlyList<string> Applied, IReadOnlyList<string> UnknownElementIds);
