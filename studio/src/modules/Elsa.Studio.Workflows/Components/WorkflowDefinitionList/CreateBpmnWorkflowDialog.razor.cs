using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
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
/// <remarks>
/// Closing the dialog while the name is checked creates nothing. Once the create is sent, the dialog cannot be closed
/// until it answers, so a workflow the server created is always opened.
/// </remarks>
public partial class CreateBpmnWorkflowDialog : IDisposable
{
    private readonly WorkflowMetadataModel _metadataModel = new();
    private readonly CancellationTokenSource _lifetime = new();
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
        // Set before the name check, which is asynchronous, so a second submit during it cannot create a second workflow.
        if (_isCreating)
        {
            return;
        }

        _isCreating = true;

        try
        {
            if (!await _submitValidator.ValidateAndPublishAsync() || _lifetime.IsCancellationRequested)
            {
                return;
            }

            var options = MudDialog.Options;
            await MudDialog.SetOptionsAsync(options with { CloseOnEscapeKey = false, CloseButton = false, BackdropClick = false });
            var result = await CreateAsync();
            await MudDialog.SetOptionsAsync(options);
            MudDialog.Close(result);
        }
        finally
        {
            _isCreating = false;
        }
    }

    /// <summary>Creates the workflow, reporting a request that never got an answer as a failure rather than an exception.</summary>
    private async Task<Result<BpmnXmlSaveResult, BpmnDocumentFailure>> CreateAsync()
    {
        try
        {
            return await BpmnXmlService.CreateAsync(_metadataModel.Name!, _lifetime.Token);
        }
        catch (HttpRequestException exception)
        {
            return new(new BpmnDocumentFailure(BpmnDocumentFailureReason.Unknown, exception.Message));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
        _submitValidator?.Dispose();
    }
}
