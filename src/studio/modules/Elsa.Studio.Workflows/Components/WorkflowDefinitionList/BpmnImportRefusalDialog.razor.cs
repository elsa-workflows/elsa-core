using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionList;

/// <summary>
/// Shows the capability names and offending element ids a BPMN import's <c>422</c> "missing host capabilities"
/// refusal carries. Read-only: it only closes, it does not let the user retry or proceed anyway.
/// </summary>
public partial class BpmnImportRefusalDialog
{
    /// <summary>
    /// The parsed refusal to display.
    /// </summary>
    [Parameter] public BpmnCapabilityRefusal Refusal { get; set; } = new([], []);

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private void OnCloseClicked() => MudDialog.Close(DialogResult.Ok(true));
}
