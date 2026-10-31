using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// Hands <see cref="ChildContent"/> the <see cref="BpmnModelerSession"/> an editor cascades under
/// <see cref="SessionCascadeName"/>, or <see langword="null"/> where none is, such as in the instance viewer.
/// </summary>
/// <remarks>
/// <see cref="BpmnDiagramDesigner"/> is not a component, so it cannot take a cascading value itself. It renders this
/// instead, which lets it key the canvas on the session: the editor replaces the designer every time it reloads the
/// workflow, and a canvas keyed on the session survives that with its undo history.
/// </remarks>
public sealed class BpmnModelerScope : ComponentBase
{
    /// <summary>The name an editor cascades its <see cref="BpmnModelerSession"/> under.</summary>
    public const string SessionCascadeName = "BpmnModelerSession";

    /// <summary>The session, when the editor edits the workflow's document in a BPMN designer.</summary>
    [CascadingParameter(Name = SessionCascadeName)] public BpmnModelerSession? Session { get; set; }

    /// <summary>What to render, given the session or <see langword="null"/>.</summary>
    [Parameter] public RenderFragment<BpmnModelerSession?> ChildContent { get; set; } = null!;

    /// <inheritdoc />
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) => builder.AddContent(0, ChildContent(Session));
}
