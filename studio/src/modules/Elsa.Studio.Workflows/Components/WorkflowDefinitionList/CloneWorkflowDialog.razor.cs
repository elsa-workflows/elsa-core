using Blazilla;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionList;

/// <summary>
/// A dialog that allows the user to clone a workflow.
/// </summary>
public partial class CloneWorkflowDialog : IDisposable
{
    private readonly WorkflowMetadataModel _metadataModel = new();
    private EditContext _editContext = null!;
    private WorkflowPropertiesModelValidator _validator = null!;
    private WorkflowMetadataSubmitValidator _submitValidator = null!;

    /// <summary>
    /// The name of the workflow to create.
    /// </summary>
    [Parameter] public string WorkflowName { get; set; } = null!;
    [Parameter] public string? WorkflowDescription { get; set; }
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _metadataModel.Name = WorkflowName;
        _metadataModel.Description = WorkflowDescription;
        _editContext = new(_metadataModel);
        _validator = new(WorkflowDefinitionService, Localizer);

        // Discard the previous instance's field-changed subscription before it is replaced, so it does not
        // keep running against an edit context this component no longer uses.
        _submitValidator?.Dispose();
        _submitValidator = new(_validator, _metadataModel, _editContext);
    }

    private Task OnCancelClicked()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private Task OnSubmitClicked() => ValidateAndSubmitAsync();

    // Blazilla runs the async uniqueness rule outside the synchronous validation pass, so the form is
    // routed through OnSubmit: OnValidSubmit would fire before that rule completed and could clone a
    // workflow under a name that turns out to be taken.
    private Task OnSubmit(EditContext _) => ValidateAndSubmitAsync();

    private async Task ValidateAndSubmitAsync()
    {
        // Validate directly against the component-owned validator rather than EditContext.ValidateAsync(),
        // whose shared, unversioned message store can be overwritten by Blazilla's own asynchronous
        // field-change validation resolving after this check does. The validator also discards a result
        // that no longer describes the current name, so a bound-name change while the check was pending
        // never submits a stale value; the user's next submission re-validates the current value.
        var isValid = await _submitValidator.ValidateAndPublishAsync();

        if (!isValid)
            return;

        MudDialog.Close(_metadataModel);
    }

    /// <inheritdoc />
    public void Dispose() => _submitValidator?.Dispose();
}
