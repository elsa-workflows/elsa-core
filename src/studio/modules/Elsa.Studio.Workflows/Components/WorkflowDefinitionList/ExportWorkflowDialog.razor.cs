using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionList;

/// <summary>
/// A dialog that allows the user to configure export options before exporting workflow definitions.
/// </summary>
public partial class ExportWorkflowDialog
{
    private bool _includeConsumingWorkflows;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private void OnCancelClicked()
    {
        MudDialog.Cancel();
    }

    private void OnExportClicked()
    {
        MudDialog.Close(DialogResult.Ok(_includeConsumingWorkflows));
    }
}

