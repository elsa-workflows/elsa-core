namespace Elsa.Studio.Bpmn.Designers;

/// <summary>A problem a designer's <c>applyMarkers</c> marks on an element.</summary>
/// <param name="ElementId">The BPMN id of the element the problem is about.</param>
/// <param name="Severity">How serious the problem is: <see cref="Error"/> or <see cref="Warning"/>.</param>
/// <param name="Message">What is wrong.</param>
public sealed record BpmnDesignerMarker(string ElementId, string Severity, string Message)
{
    /// <summary>The severity of a problem that stops the document from being saved or run.</summary>
    public const string Error = "error";

    /// <summary>The severity of a problem the document can be saved with.</summary>
    public const string Warning = "warning";
}
