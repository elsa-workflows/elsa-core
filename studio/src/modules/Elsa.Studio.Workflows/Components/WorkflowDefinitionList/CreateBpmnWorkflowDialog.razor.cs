using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionList;

/// <summary>
/// Asks for the name of a new BPMN workflow and creates it through <see cref="IBpmnXmlService.CreateAsync"/>: a draft
/// holding a minimal process, to be drawn in the enabled BPMN designer. Closes with the create result.
/// </summary>
public partial class CreateBpmnWorkflowDialog : IDisposable
{
    private readonly WorkflowMetadataModel _metadataModel = new();
    private EditContext _editContext = null!;
    private WorkflowPropertiesModelValidator _validator = null!;
    private WorkflowMetadataSubmitValidator _submitValidator = null!;
    private bool _isCreating;

    /// <summary>The name to start from.</summary>
    [Parameter] public string WorkflowName { get; set; } = "New workflow";
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;
    [Inject] private IBpmnXmlService BpmnXmlService { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _metadataModel.Name = WorkflowName;
        _editContext = new(_metadataModel);
        _validator = new(WorkflowDefinitionService, Localizer);
        _submitValidator?.Dispose();
        _submitValidator = new(_validator, _metadataModel, _editContext);
    }

    private void OnCancelClicked() => MudDialog.Cancel();

    private Task OnSubmitClicked() => ValidateAndCreateAsync();

    // Routed through OnSubmit for the same reason as CreateWorkflowDialog: the name's uniqueness rule is asynchronous.
    private Task OnSubmit(EditContext _) => ValidateAndCreateAsync();

    private async Task ValidateAndCreateAsync()
    {
        if (_isCreating || !await _submitValidator.ValidateAndPublishAsync())
            return;

        _isCreating = true;

        try
        {
            MudDialog.Close(await BpmnXmlService.CreateAsync(_metadataModel.Name!));
        }
        finally
        {
            _isCreating = false;
        }
    }

    /// <inheritdoc />
    public void Dispose() => _submitValidator?.Dispose();
}
