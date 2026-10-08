using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Shared.Components;

/// <summary>
/// Prompts for the root activity to place in an empty embedded activity port.
/// </summary>
public partial class SelectWorkflowRootActivityDialog
{
    private IReadOnlyCollection<WorkflowRootActivityTemplate> _templates = [];
    private string? _selectedTemplateKey;

    /// <summary>
    /// Gets or sets the initially selected root activity template key.
    /// </summary>
    [Parameter] public string? SelectedTemplateKey { get; set; }

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IWorkflowRootActivityTemplateProvider TemplateProvider { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _templates = TemplateProvider.List();
        _selectedTemplateKey = TemplateProvider.Find(SelectedTemplateKey)?.Key ?? TemplateProvider.GetDefault().Key;
    }

    private void Cancel() => MudDialog.Cancel();

    private void Submit() => MudDialog.Close(DialogResult.Ok(_selectedTemplateKey));
}
