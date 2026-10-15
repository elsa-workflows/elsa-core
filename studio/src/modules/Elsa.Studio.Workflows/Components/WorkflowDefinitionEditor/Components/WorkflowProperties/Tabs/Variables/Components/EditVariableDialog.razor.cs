using Blazilla;
using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Variables.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Variables.Validators;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Variables.Components;

/// <summary>
/// A dialog that allows the user to edit a variable.
/// </summary>
public partial class EditVariableDialog
{
    private readonly VariableModel _model = new();
    private EditContext _editContext = null!;
    private VariableModelValidator _validator = null!;
    private ICollection<StorageDriverDescriptor> _storageDriverDescriptors = new List<StorageDriverDescriptor>();
    private ICollection<VariableTypeDescriptor> _variableTypes = new List<VariableTypeDescriptor>();
    private ICollection<IGrouping<string, VariableTypeDescriptor>> _groupedVariableTypes = new List<IGrouping<string, VariableTypeDescriptor>>();

    /// <summary>
    /// The workflow definition that the variable belongs to.
    /// </summary>
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    /// <summary>
    /// The variable to edit. If null, a new variable will be created.
    /// </summary>
    [Parameter] public Variable? Variable { get; set; }
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IStorageDriverService StorageDriverService { get; set; } = null!;
    [Inject] private IVariableTypeService VariableTypeService { get; set; } = null!;
    [Inject] private IIdentityGenerator IdentityGenerator { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        // Instantiate the edit context first, so that it is available when rendering (which happens as soon as we call an async method on the next lines). 
        _editContext = new(_model);
        _validator = new(WorkflowDefinition, Localizer);
        
        _storageDriverDescriptors = (await StorageDriverService.GetStorageDriversAsync()).OrderByDescending(x => x.Priority).ToList();
        _variableTypes = (await VariableTypeService.GetVariableTypesAsync()).ToList();
        _groupedVariableTypes = _variableTypes.GroupBy(x => x.Category).ToList();

        if (Variable == null)
        {
            _model.Id = IdentityGenerator.GenerateId();
            _model.Name = GetNewVariableName(WorkflowDefinition.Variables);
            _model.StorageDriver = _storageDriverDescriptors.First();
            _model.VariableType = _variableTypes.First();
        }
        else
        {
            _model.Id = Variable.Id;
            _model.Name = Variable.Name;
            _model.VariableType = _variableTypes.FirstOrDefault(x => x.TypeName == Variable.TypeName) ?? _variableTypes.First();
            _model.DefaultValue = Variable.Value?.ToString() ?? "";
            _model.IsArray = Variable.IsArray;
            _model.StorageDriver = _storageDriverDescriptors.FirstOrDefault(x => x.TypeName == Variable.StorageDriverTypeName) ?? _storageDriverDescriptors.First();
        }
    }

    private string GetNewVariableName(ICollection<Variable> existingVariables)
    {
        var count = 0;

        while (true)
        {
            var variableName = $"Variable{++count}";

            if (existingVariables.All(x => x.Name != variableName))
                return variableName;
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
        var variable = Variable ?? new Variable();

        variable.Id = _model.Id;
        variable.Name = _model.Name;
        variable.TypeName = _model.VariableType.TypeName;
        variable.Value = _model.DefaultValue;
        variable.IsArray = _model.IsArray;
        variable.StorageDriverTypeName = _model.StorageDriver.TypeName;

        MudDialog.Close(variable);
        return Task.CompletedTask;
    }
}