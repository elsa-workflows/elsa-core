using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionList;

/// <summary>
/// Shows the Info/Degraded/Dropped findings a BPMN import would produce, grouped by severity with each finding's
/// element id, and -- when the document declares more than one process -- collects which one to import. Closes
/// with the chosen process id (or <see langword="null"/> when the document declares exactly one) on confirmation.
/// </summary>
public partial class BpmnImportFindingsDialog
{
    private string? _selectedProcessId;

    /// <summary>
    /// The analysis to display.
    /// </summary>
    [Parameter] public BpmnImportAnalysisModel Analysis { get; set; } = new();

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private bool RequiresProcessSelection => Analysis.ProcessIds.Count > 1;
    private bool CanConfirm => !RequiresProcessSelection || !string.IsNullOrEmpty(_selectedProcessId);

    private IReadOnlyList<IssueGroup> Groups => new[]
    {
        new IssueGroup(Localizer["Dropped"], Color.Error, IssuesOfSeverity(BpmnImportIssueSeverity.Dropped)),
        new IssueGroup(Localizer["Degraded"], Color.Warning, IssuesOfSeverity(BpmnImportIssueSeverity.Degraded)),
        new IssueGroup(Localizer["Info"], Color.Info, IssuesOfSeverity(BpmnImportIssueSeverity.Info))
    }.Where(group => group.Issues.Count > 0).ToList();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Analysis.ProcessIds.Count == 1)
            _selectedProcessId = Analysis.ProcessIds.First();
    }

    private IReadOnlyList<BpmnImportIssueModel> IssuesOfSeverity(string severity) =>
        Analysis.Issues.Where(issue => string.Equals(issue.Severity, severity, StringComparison.OrdinalIgnoreCase)).ToList();

    private void OnCancelClicked() => MudDialog.Cancel();

    private void OnConfirmClicked() => MudDialog.Close(DialogResult.Ok(_selectedProcessId));

    private sealed record IssueGroup(string Title, Color Color, IReadOnlyList<BpmnImportIssueModel> Issues);
}
