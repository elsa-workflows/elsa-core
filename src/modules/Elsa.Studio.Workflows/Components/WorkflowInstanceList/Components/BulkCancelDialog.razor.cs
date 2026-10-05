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

    /// <summary>
    /// Whether to offer cancelling every matching instance, which requires permission to submit alteration plans.
    /// </summary>
    [Parameter] public bool CanApplyToAllMatches { get; set; } = true;

    /// <summary>
    /// Whether the user can cancel the selected instances directly. Without it, only cancelling every match is offered.
    /// </summary>
    [Parameter] public bool CanCancelSelected { get; set; } = true;

    private void Submit() => MudDialog.Close(DialogResult.Ok(ApplyToAllMatches || !CanCancelSelected));

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());
}