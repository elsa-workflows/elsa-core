namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>The canvas a <see cref="BpmnModelerSession"/> saves from: the designer's own <c>save</c>, <c>undo</c> and <c>redo</c>.</summary>
internal interface IBpmnModelerCanvas
{
    /// <summary>Exports the designer's document as BPMN XML. The designer counts it as saved from then on.</summary>
    Task<string> SaveAsync();

    /// <summary>Undoes the designer's last edit.</summary>
    Task UndoAsync();

    /// <summary>Redoes the designer's last undone edit.</summary>
    Task RedoAsync();
}
