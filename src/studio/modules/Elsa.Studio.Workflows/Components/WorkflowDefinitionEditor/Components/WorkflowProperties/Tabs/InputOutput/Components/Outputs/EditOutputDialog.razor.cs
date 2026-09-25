using Blazilla;
using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Validators;
using Elsa.Studio.Workflows.Domain.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Components.Outputs;

/// <summary>
/// A dialog for editing an output.
/// </summary>
public partial class EditOutputDialog
{
    private readonly OutputDefinitionModel _model = new();
    private EditContext _editContext = null!;
    private OutputModelValidator _validator = null!;
    private ICollection<StorageDriverDescriptor> _storageDriverDescriptors = new List<StorageDriverDescriptor>();
    private ICollection<VariableTypeDescriptor> _variableTypes = new List<VariableTypeDescriptor>();
    private ICollection<IGrouping<string, VariableTypeDescriptor>> _groupedVariableTypes = new List<IGrouping<string, VariableTypeDescriptor>>();

    /// <summary>
    /// The workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    
    /// <summary>
    /// The output to edit.
    /// </summary>
    [Parameter] public OutputDefinition? Output { get; set; }
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IStorageDriverService StorageDriverService { get; set; } = null!;
    [Inject] private IVariableTypeService VariableTypeService { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        // Instantiate the edit context first so that it is available when rendering (which happens as soon as we call an async method on the next line). 
        _editContext = new(_model);
        _validator = new(WorkflowDefinition, Localizer);

        _storageDriverDescriptors = (await StorageDriverService.GetStorageDriversAsync()).ToList();
        _variableTypes = (await VariableTypeService.GetVariableTypesAsync()).ToList();
        _groupedVariableTypes = _variableTypes.GroupBy(x => x.Category).ToList();

        if (Output == null)
        {
            _model.Name = GetNewOutputName(WorkflowDefinition.Outputs);
            _model.DisplayName = _model.Name.Humanize();
            _model.Type = _variableTypes.First();
        }
        else
        {
            _model.Name = Output.Name;
            _model.Type = _variableTypes.FirstOrDefault(x => x.TypeName == Output.Type) ?? _variableTypes.First();
            _model.IsArray = Output.IsArray;
            _model.Description = Output.Description;
            _model.Category = Output.Category;
            _model.DisplayName = Output.DisplayName;
        }
    }

    private string GetNewOutputName(ICollection<OutputDefinition> existingOutputs)
    {
        var count = 0;

        while (true)
        {
            var inputName = $"Output{++count}";

            if (existingOutputs.All(x => x.Name != inputName))
                return inputName;
        }
    }
    
    private Task OnCancelClicked()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private async Task OnSubmitClicked()
    {
        if (!await _editContext.ValidateAsync())
            return;

        await OnValidSubmit();
    }

    private Task OnValidSubmit()
    {
        var output = Output ?? new OutputDefinition();

        output.Name = _model.Name;
        output.Type = _model.Type.TypeName;
        output.IsArray = _model.IsArray;
        output.Category = _model.Type.Category;
        output.Description = _model.Description;
        output.DisplayName = _model.DisplayName;

        MudDialog.Close(output);
        return Task.CompletedTask;
    }
}