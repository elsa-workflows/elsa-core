namespace Elsa.Studio.Bpmn.Designers;

/// <summary>A designer's <c>selection</c> event.</summary>
/// <param name="ElementIds">The ids of every selected element; empty when nothing is selected.</param>
/// <param name="Primary">The element the selection is about, or null when nothing is selected.</param>
public sealed record BpmnDesignerSelection(IReadOnlyList<string> ElementIds, BpmnDesignerSelectedElement? Primary);

/// <summary>The primary element of a <see cref="BpmnDesignerSelection"/>.</summary>
/// <param name="Id">The element's BPMN id.</param>
/// <param name="Type">The element's BPMN type, such as <c>bpmn:Task</c>.</param>
/// <param name="Name">The element's name, if it has one.</param>
public sealed record BpmnDesignerSelectedElement(string Id, string Type, string? Name);
