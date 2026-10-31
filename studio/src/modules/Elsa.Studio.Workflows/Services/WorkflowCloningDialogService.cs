using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionList;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Models;
using MudBlazor;

namespace Elsa.Studio.Workflows.Services;

/// <inheritdoc/>
public class WorkflowCloningDialogService(
    IWorkflowDefinitionEditorService WorkflowDefinitionEditorService,
    IUserMessageService UserMessageService,
    IDialogService DialogService,
    ILocalizer Localizer) : IWorkflowCloningDialogService
{
    /// <inheritdoc/>
    public async Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>?> Duplicate(WorkflowDefinition workflowDefinition) => await Clone(workflowDefinition, "Duplicate");

    /// <inheritdoc/>
    public async Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>?> SaveAs(WorkflowDefinition workflowDefinition) => await Clone(workflowDefinition, "Save As");
        
    /// <summary>
    /// Creates a clone of a workflow definition
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition to clone. Cannot be <see langword="null"/>.</param>
    /// <param name="type">A string representing the type of the cloned workflow, used to modify the name of the new workflow.</param>
    /// <returns>The task result contains a <see cref="Result{TSuccess,TFailure}"/> object that encapsulates the response of saving the cloned workflow definition
    /// or <see langword="null"/> if the operation is canceled.</returns>
    private async Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>?> Clone(WorkflowDefinition workflowDefinition, string type)
    {
        var newWorkflowName = $"{workflowDefinition.Name} - {type} of {workflowDefinition.DefinitionId}";
        var parameters = new DialogParameters<CloneWorkflowDialog>
        {
            { x => x.WorkflowName, newWorkflowName },
            { x => x.WorkflowDescription, workflowDefinition.Description }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };

        var dialogInstance = await DialogService.ShowAsync<CloneWorkflowDialog>(Localizer[type], parameters, options);
        var dialogResult = await dialogInstance.Result;
        if (dialogResult?.Canceled ?? true)
        {
            return null;
        }

        var newWorkflowModel = (WorkflowMetadataModel?)dialogResult.Data;
        var newDefinition = new WorkflowDefinition
        {
            Name = newWorkflowModel?.Name ?? newWorkflowName,
            Description = newWorkflowModel?.Description,
            Root = workflowDefinition.Root,
            Inputs = workflowDefinition.Inputs,
            Outputs = workflowDefinition.Outputs,
            Variables = workflowDefinition.Variables,
            Options = workflowDefinition.Options,
            Outcomes = workflowDefinition.Outcomes,
            CustomProperties = workflowDefinition.CustomProperties,
            IsReadonly = false
        };

        var result = await WorkflowDefinitionEditorService.SaveAsync(newDefinition, false, async definition =>
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer[$"{type} successfully saved."], Severity.Success);
        });

        if (result.IsFailed) UserMessageService.ShowSnackbarTextMessage(string.Join(Environment.NewLine, result.Failure?.Errors ?? []), Severity.Error);
        return result;
    }
}
