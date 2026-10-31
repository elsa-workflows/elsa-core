using System.Net;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
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
public class WorkflowDefinitionEditorService(IBackendApiClientProvider backendApiClientProvider, IMediator mediator) : IWorkflowDefinitionEditorService
{
    /// <inheritdoc />
    public async Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveAsync(WorkflowDefinition workflowDefinition, bool publish, Func<WorkflowDefinition, Task>? workflowSavedCallback = null, CancellationToken cancellationToken = default)
    {
        var request = new SaveWorkflowDefinitionRequest
        {
            Model = new WorkflowDefinitionModel
            {
                Id = workflowDefinition.Id,
                Description = workflowDefinition.Description,
                Name = workflowDefinition.Name,
                ToolVersion = workflowDefinition.ToolVersion,
                Inputs = workflowDefinition.Inputs,
                Options = workflowDefinition.Options,
                Outcomes = workflowDefinition.Outcomes,
                Outputs = workflowDefinition.Outputs,
                Variables = workflowDefinition.Variables.Select(x => new VariableDefinition
                {
                    Id = x.Id,
                    Name = x.Name,
                    TypeName = x.TypeName,
                    Value = x.Value?.ToString(),
                    IsArray = x.IsArray,
                    StorageDriverTypeName = x.StorageDriverTypeName
                }).ToList(),
                Version = workflowDefinition.Version,
                CreatedAt = workflowDefinition.CreatedAt,
                CustomProperties = workflowDefinition.CustomProperties,
                DefinitionId = workflowDefinition.DefinitionId,
                IsLatest = workflowDefinition.IsLatest,
                IsPublished = workflowDefinition.IsPublished,
                Root = workflowDefinition.Root
            },
            Publish = publish,
        };

        var api = await GetApiAsync(cancellationToken);

        try
        {
            if (request.Publish == true) await mediator.NotifyAsync(new WorkflowDefinitionPublishing(workflowDefinition), cancellationToken);
            await mediator.NotifyAsync(new WorkflowDefinitionSaving(workflowDefinition), cancellationToken);
            var response = await api.SaveAsync(request, cancellationToken);

            if (workflowSavedCallback != null)
                await workflowSavedCallback(response.WorkflowDefinition);

            await mediator.NotifyAsync(new WorkflowDefinitionSaved(response.WorkflowDefinition), cancellationToken);
            if (request.Publish == true) await mediator.NotifyAsync(new WorkflowDefinitionPublished(response.WorkflowDefinition), cancellationToken);
            return new(response);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.BadRequest)
        {
            var errors = e.GetValidationErrors();
            await mediator.NotifyAsync(new WorkflowDefinitionSavingFailed(workflowDefinition, errors), cancellationToken);
            if (request.Publish == true) await mediator.NotifyAsync(new WorkflowDefinitionPublishingFailed(workflowDefinition, errors), cancellationToken);
            return new(errors);
        }
    }

    /// <inheritdoc />
    public async Task<SaveWorkflowDefinitionResponse> PublishAsync(WorkflowDefinition workflowDefinition, Func<WorkflowDefinition, Task>? workflowPublishedCallback = null, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        await mediator.NotifyAsync(new WorkflowDefinitionPublishing(workflowDefinition), cancellationToken);
        var response = await api.PublishAsync(workflowDefinition.DefinitionId, new PublishWorkflowDefinitionRequest(), cancellationToken);
        if (workflowPublishedCallback != null) await workflowPublishedCallback(workflowDefinition);
        await mediator.NotifyAsync(new WorkflowDefinitionPublished(workflowDefinition), cancellationToken);
        return response;
    }

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
    public async Task<FileDownload> ExportAsync(WorkflowDefinition workflowDefinition, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        await mediator.NotifyAsync(new WorkflowDefinitionExporting(workflowDefinition), cancellationToken);
        var response = await api.ExportAsync(workflowDefinition.DefinitionId, VersionOptions.SpecificVersion(workflowDefinition.Version), includeConsumingWorkflows, cancellationToken);
        var fileName = response.GetDownloadedFileNameOrDefault($"workflow-definition-{workflowDefinition.DefinitionId}-v{workflowDefinition.Version}.json");
        var fileDownload = new FileDownload(fileName, response.Content!);
        await mediator.NotifyAsync(new WorkflowDefinitionExported(workflowDefinition, fileDownload), cancellationToken);

        return fileDownload;
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
