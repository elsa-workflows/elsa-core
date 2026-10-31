using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Components.Outputs;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Variables.Components;

/// <summary>
/// Represents the variables tab.
/// </summary>
public partial class VariablesTab
{
    private ICollection<StorageDriverDescriptor> _storageDriverDescriptors = new List<StorageDriverDescriptor>();

    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public EventCallback WorkflowDefinitionUpdated { get; set; }
    [CascadingParameter] public IWorkspace? Workspace { get; set; }
    [Inject] IStorageDriverService StorageDriverService { get; set; } = default!;
    [Inject] IDialogService DialogService { get; set; } = default!;

    private bool IsReadOnly => Workspace?.IsReadOnly ?? true;
    private ICollection<Variable> Variables => WorkflowDefinition.Variables;
    private IDictionary<string, object?> VariableTestValues => WorkflowDefinition.GetVariableTestValues();

    protected override async Task OnInitializedAsync()
    {
        _storageDriverDescriptors = (await StorageDriverService.GetStorageDriversAsync()).ToList();
    }

    private StorageDriverDescriptor? GetStorageDriverDescriptor(string typeName)
    {
        return _storageDriverDescriptors.FirstOrDefault(x => x.TypeName == typeName);
    }

    private async Task RaiseWorkflowDefinitionUpdatedAsync()
    {
        if (WorkflowDefinitionUpdated.HasDelegate)
            await WorkflowDefinitionUpdated.InvokeAsync();
    }

    private async Task OpenVariableEditorDialog(Variable? variable)
    {
        var isNew = variable == null;

        var parameters = new DialogParameters<EditVariableDialog>
        {
            [nameof(EditVariableDialog.WorkflowDefinition)] = WorkflowDefinition
        };

        if (!isNew)
            parameters[nameof(EditVariableDialog.Variable)] = variable;

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var title = variable == null ? Localizer["Create variable"] : Localizer["Edit variable"];
        var dialog = await DialogService.ShowAsync<EditVariableDialog>(title, parameters, options);
        var result = await dialog.Result;

        if (result?.Canceled != false)
            return;

        if (isNew && result.Data is Variable newVariable)
            WorkflowDefinition.Variables.Add(newVariable);

        await RaiseWorkflowDefinitionUpdatedAsync();
    }
    
    private async Task OpenVariableTestValueEditorDialog(Variable variable)
    {
        var parameters = new DialogParameters<EditVariableTestValueDialog>
        {
            [nameof(EditVariableDialog.WorkflowDefinition)] = WorkflowDefinition,
            [nameof(EditVariableDialog.Variable)] = variable
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var title = Localizer["Set Variable Test Value"];
        var dialog = await DialogService.ShowAsync<EditVariableTestValueDialog>(title, parameters, options);
        var result = await dialog.Result;

        if (result?.Canceled == true)
            return;
        
        await RaiseWorkflowDefinitionUpdatedAsync();
    }

    private async Task OnEditClicked(Variable variable)
    {
        await OpenVariableEditorDialog(variable);
    }

    private async Task OnDeleteClicked(Variable variable)
    {
        var result = await DialogService.ShowMessageBoxAsync(Localizer["Delete selected variable?"], Localizer["Are you sure you want to delete the selected variable?"], yesText: Localizer["Delete"], cancelText: Localizer["Cancel"]);

        if (result != true)
            return;

        WorkflowDefinition.Variables.Remove(variable);

        await RaiseWorkflowDefinitionUpdatedAsync();
    }

    private async Task OnAddVariableClicked()
    {
        await OpenVariableEditorDialog(null);
    }

    private async Task OnTestValueButtonClick(Variable variable)
    {
        await OpenVariableTestValueEditorDialog(variable);
    }
}
