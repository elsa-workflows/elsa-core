using System.Net;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Notifications;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// A workflow definition service that uses a remote backend to retrieve workflow definitions.
/// </summary>
public class WorkflowDefinitionHistoryService(IBackendApiClientProvider backendApiClientProvider, IMediator mediator) : IWorkflowDefinitionHistoryService
{
    /// <inheritdoc />
    public async Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(WorkflowDefinition workflowDefinition, Func<WorkflowDefinition, Task>? workflowRetractedCallback = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var api = await GetApiAsync(cancellationToken);
            await mediator.NotifyAsync(new WorkflowDefinitionRetracting(workflowDefinition), cancellationToken);
            var definition = await api.RetractAsync(workflowDefinition.DefinitionId, new RetractWorkflowDefinitionRequest(), cancellationToken);
            if (workflowRetractedCallback != null) await workflowRetractedCallback(definition);
            await mediator.NotifyAsync(new WorkflowDefinitionRetracted(definition), cancellationToken);
            return new(definition);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.BadRequest)
        {
            var errors = e.GetValidationErrors();
            await mediator.NotifyAsync(new WorkflowDefinitionRetractingFailed(workflowDefinition, errors), cancellationToken);
            return new(errors);
        }
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinitionSummary> RevertAsync(WorkflowDefinitionVersion workflowDefinitionVersion, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        await mediator.NotifyAsync(new WorkflowDefinitionVersionReverting(workflowDefinitionVersion), cancellationToken);
        var newWorkflowDefinitionSummary = await api.RevertVersionAsync(workflowDefinitionVersion.WorkflowDefinitionId, workflowDefinitionVersion.Version, cancellationToken);
        var newWorkflowDefinitionVersion = WorkflowDefinitionVersion.FromDefinitionSummary(newWorkflowDefinitionSummary);
        await mediator.NotifyAsync(new WorkflowDefinitionVersionReverted(newWorkflowDefinitionVersion), cancellationToken);
        return newWorkflowDefinitionSummary;
    }

    private async Task<IWorkflowDefinitionsApi> GetApiAsync(CancellationToken cancellationToken = default)
    {
        return await backendApiClientProvider.GetApiAsync<IWorkflowDefinitionsApi>(cancellationToken);
    }
}
