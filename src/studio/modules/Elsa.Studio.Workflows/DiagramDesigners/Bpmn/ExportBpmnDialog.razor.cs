using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// A confirmation dialog shown before a BPMN export download, disclosing that the exported file carries the
/// definition's binding configuration and expressions.
/// </summary>
public partial class ExportBpmnDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private void OnCancelClicked() => MudDialog.Cancel();

    private void OnExportClicked() => MudDialog.Close(DialogResult.Ok(true));
}
