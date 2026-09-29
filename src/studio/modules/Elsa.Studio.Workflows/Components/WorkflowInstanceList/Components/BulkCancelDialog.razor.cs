using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceList.Components;

/// <summary>
/// Represents the bulk cancel dialog.
/// </summary>
public partial class BulkCancelDialog : ComponentBase
{
    private bool ApplyToAllMatches { get; set; }
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private void Submit() => MudDialog.Close(DialogResult.Ok(ApplyToAllMatches));

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());
}