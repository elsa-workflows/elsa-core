namespace Elsa.Studio.Bpmn.Designers;

/// <summary>A designer's <c>change</c> event: the document or its undo state changed.</summary>
/// <param name="ElementIds">The ids of the elements the change touched.</param>
/// <param name="IsDirty">Whether the document differs from the one last loaded or saved.</param>
/// <param name="CanUndo">Whether there is an edit to undo.</param>
/// <param name="CanRedo">Whether there is an undone edit to redo.</param>
public sealed record BpmnDesignerChange(IReadOnlyList<string> ElementIds, bool IsDirty, bool CanUndo, bool CanRedo);
