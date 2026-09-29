using Elsa.Api.Client.Resources.WorkflowDefinitions.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Variables.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Variables.Components;

/// <summary>
/// A dialog that allows the user to edit a variable.
/// </summary>
public partial class EditVariableTestValueDialog
{
    private readonly VariableTestValueModel _model = new();
    private DataPanelModel _variableData = new();
    private EditContext _editContext = null!;

    /// <summary>
    /// The workflow definition that the variable belongs to.
    /// </summary>
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    /// <summary>
    /// The variable to edit. If null, a new variable will be created.
    /// </summary>
    [Parameter] public Variable Variable { get; set; } = null!;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _editContext = new(_model);
        _model.VariableId = Variable.Id;
        _model.TestValue = WorkflowDefinition.GetVariableTestValue(Variable.Id)?.ToString() ?? "";

        _variableData = new();
        _variableData.Add("Variable ID", Variable.Id);
        _variableData.Add("Name", Variable.Name);
        _variableData.Add("Type", Variable.TypeName);
        _variableData.Add("Default Value", Variable.Value?.ToString());
    }

    private Task OnCancelClicked()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private void OnSubmit()
    {
        if(_model.TestValue == null)
            WorkflowDefinition.ClearVariableTestValue(Variable.Id);
        else
            WorkflowDefinition.SetVariableTestValue(Variable.Id, _model.TestValue);
        MudDialog.Close();
    }

    private void OnClearTestValueClick()
    {
        _model.TestValue = null;
        StateHasChanged();
    }
}