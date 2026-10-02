using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Components.Outputs;

/// <summary>
/// Represents the outputs section.
/// </summary>
public partial class OutputsSection
{
    private ICollection<VariableTypeDescriptor> _variableTypes = new List<VariableTypeDescriptor>();
    private ICollection<StorageDriverDescriptor> _storageDriverDescriptors = new List<StorageDriverDescriptor>();

    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public EventCallback WorkflowDefinitionUpdated { get; set; }
    [Parameter] public bool IsReadonly { get; set; } = default;
    [Inject] private IStorageDriverService StorageDriverService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IVariableTypeService VariableTypeService { get; set; } = default!;

    private ICollection<OutputDefinition> Outputs => WorkflowDefinition.Outputs;

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

    private async Task OpenOutputEditorDialog(OutputDefinition? outputDefinition)
    {
        var isNew = outputDefinition == null;

        var parameters = new DialogParameters<EditOutputDialog>
        {
            [nameof(EditOutputDialog.WorkflowDefinition)] = WorkflowDefinition
        };

        if (!isNew)
            parameters[nameof(EditOutputDialog.Output)] = outputDefinition;

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var title = outputDefinition == null ? Localizer["Create output"] : Localizer["Edit output"];
        var dialog = await DialogService.ShowAsync<EditOutputDialog>(title, parameters, options);
        var result = await dialog.Result;

        if (result?.Canceled != false)
            return;

        if (isNew && result.Data is OutputDefinition newOutput)
            WorkflowDefinition.Outputs.Add(newOutput);

        await RaiseWorkflowDefinitionUpdatedAsync();
    }

    private async Task OnEditClicked(OutputDefinition input)
    {
        await OpenOutputEditorDialog(input);
    }

    private async Task OnDeleteClicked(OutputDefinition input)
    {
        var result = await DialogService.ShowMessageBoxAsync(Localizer["Delete selected output?"], Localizer["Are you sure you want to delete the selected output?"], yesText: Localizer["Delete"], cancelText: Localizer["Cancel"]);

        if (result != true)
            return;

        WorkflowDefinition.Outputs.Remove(input);

        await RaiseWorkflowDefinitionUpdatedAsync();
    }

    private async Task OnAddOutputClicked()
    {
        await OpenOutputEditorDialog(null);
    }
}
