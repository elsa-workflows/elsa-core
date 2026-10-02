using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;

/// <summary>
/// Represents the workflow editor component base.
/// </summary>
public abstract class WorkflowEditorComponentBase : StudioComponentBase
{
    /// <summary>
    /// Provides the  workflow definition.
    /// </summary>
    protected WorkflowDefinition? _workflowDefinition;
    /// <summary>
    /// Indicates whether is progressing.
    /// </summary>
    protected bool IsProgressing { get; set; }

    /// <summary>An event that is invoked when a workflow definition has been executed.</summary>
    /// <remarks>The ID of the workflow instance is provided as the value to the event callback.</remarks>
    [Parameter] public Func<string, Task>? WorkflowDefinitionExecuted { get; set; }
    
    /// <summary>
    /// The injected workflow definition service.
    /// </summary>
    [Inject] protected IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private ILocalizer Localizer { get; set; } = default!;
    
    /// <summary>
    /// The injected navigation manager.
    /// </summary>
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// The injected user message service.
    /// </summary>
    [Inject] protected IUserMessageService UserMessageService { get; set; } = default!;

    /// <summary>
    /// Invoked when the user clicked the "Run Workflow" button.
    /// </summary>
    protected async Task OnRunWorkflowClicked()
    {
        var executeResult = await ProgressAsync(async () =>
        {
            var request = new ExecuteWorkflowDefinitionRequest
            {
                VersionOptions = VersionOptions.Latest
            };

            var definitionId = _workflowDefinition!.DefinitionId;
            return await WorkflowDefinitionService.ExecuteAsync(definitionId, request);
        });

        if (executeResult.CannotStart)
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer["The workflow cannot be started"], Severity.Error);
            return;
        }

        var workflowInstanceId = executeResult.WorkflowInstanceId;
        
        if (workflowInstanceId != null)
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer["Successfully started workflow"], Severity.Success);

            if (WorkflowDefinitionExecuted != null)
                await WorkflowDefinitionExecuted(workflowInstanceId);
            else
                NavigationManager.NavigateTo($"workflows/instances/{workflowInstanceId}/view");
        }
    }

    /// <summary>
    /// Helper method to show progress while executing an async operation (void return)
    /// </summary>
    protected async Task ProgressAsync(Func<Task> action)
    {
        IsProgressing = true;
        StateHasChanged();
 
        try
        {
            await action.Invoke();
        }
        finally
        {
            IsProgressing = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Shows progress while executing an asynchronous operation that returns a value.
    /// </summary>
    /// <typeparam name="T">The type of result produced by the operation.</typeparam>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <returns>The result returned by <paramref name="action"/>.</returns>
    protected async Task<T> ProgressAsync<T>(Func<Task<T>> action)
    {
        IsProgressing = true;
        StateHasChanged();
        var result = await action.Invoke();
        IsProgressing = false;
        StateHasChanged();

        return result;
    }
}