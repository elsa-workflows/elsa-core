using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionList;

/// <summary>
/// Shows the capability names and offending element ids a BPMN import's <c>422</c> "missing host capabilities"
/// refusal carries, or, when <see cref="ConditionRefusal"/> is set, the sequence flows whose expression conditions the
/// server does not evaluate. Read-only: it only closes, it does not let the user retry or proceed anyway.
/// </summary>
public partial class BpmnImportRefusalDialog
{
    /// <summary>
    /// The parsed refusal to display.
    /// </summary>
    [Parameter] public BpmnCapabilityRefusal Refusal { get; set; } = new([], []);

    /// <summary>
    /// The expression-condition refusal to display instead of <see cref="Refusal"/>, or <see langword="null"/> for a
    /// capability refusal.
    /// </summary>
    [Parameter] public BpmnConditionRefusal? ConditionRefusal { get; set; }

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private void OnCloseClicked() => MudDialog.Close(DialogResult.Ok(true));
}
