using Elsa.Studio.Bpmn.Designers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// Asks the user, before the first save of a document, to accept losing the content the BPMN designer reported it
/// cannot keep (<see cref="BpmnSaveGate.DroppedWarnings"/>). Closing it any way but "Save without it" saves nothing.
/// </summary>
public partial class BpmnDroppedContentDialog
{
    /// <summary>The designer's load warnings that report content a save would lose.</summary>
    [Parameter] public IReadOnlyList<BpmnDesignerLoadWarning> Warnings { get; set; } = [];

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private static string Describe(BpmnDesignerLoadWarning warning) =>
        warning.ElementId == null ? warning.Message : $"{warning.ElementId}: {warning.Message}";

    private void OnCancelClicked() => MudDialog.Cancel();

    private void OnSaveClicked() => MudDialog.Close(DialogResult.Ok(true));
}
