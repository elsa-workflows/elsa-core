using Elsa.Studio.Bpmn.Designers;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// The canvas a <see cref="BpmnModelerSession"/> edits through: the designer's own <c>save</c>, <c>undo</c>, <c>redo</c>,
/// <c>getBinding</c>, <c>setBinding</c>, <c>select</c> and <c>applyMarkers</c>.
/// </summary>
internal interface IBpmnModelerCanvas
{
    /// <summary>Exports the designer's document as BPMN XML. The designer counts it as saved from then on.</summary>
    Task<string> SaveAsync();

    /// <summary>Undoes the designer's last edit.</summary>
    Task UndoAsync();

    /// <summary>Redoes the designer's last undone edit.</summary>
    Task RedoAsync();

    /// <summary>The <c>elsa:activityBinding</c> of the element with <paramref name="elementId"/>, or <see langword="null"/>.</summary>
    Task<BpmnDesignerBinding?> GetBindingAsync(string elementId);

    /// <summary>Makes <paramref name="binding"/> the element's binding, as one undoable edit.</summary>
    Task<BpmnDesignerSetBindingResult> SetBindingAsync(string elementId, BpmnDesignerBinding binding);

    /// <summary>Selects the element with <paramref name="elementId"/>. Returns whether the designer has it.</summary>
    Task<bool> SelectAsync(string elementId);

    /// <summary>Marks each element a problem is about, replacing the markers shown before. A designer without markers ignores it.</summary>
    Task ApplyMarkersAsync(IReadOnlyList<BpmnDesignerMarker> markers);
}
