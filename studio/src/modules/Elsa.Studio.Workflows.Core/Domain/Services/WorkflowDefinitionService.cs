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
public class RemoteWorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly IBackendApiClientProvider backendApiClientProvider;
    private readonly IIdentityGenerator identityGenerator;
    private readonly IWorkflowRootActivityTemplateProvider workflowRootActivityTemplateProvider;
    private readonly IMediator mediator;
    private readonly IRemoteFeatureProvider? remoteFeatureProvider;

    public RemoteWorkflowDefinitionService(
        IBackendApiClientProvider backendApiClientProvider,
        IIdentityGenerator identityGenerator,
        IWorkflowRootActivityTemplateProvider workflowRootActivityTemplateProvider,
        IMediator mediator)
        : this(backendApiClientProvider, identityGenerator, workflowRootActivityTemplateProvider, mediator, null)
    {
    }

    public RemoteWorkflowDefinitionService(
        IBackendApiClientProvider backendApiClientProvider,
        IIdentityGenerator identityGenerator,
        IWorkflowRootActivityTemplateProvider workflowRootActivityTemplateProvider,
        IMediator mediator,
        IRemoteFeatureProvider? remoteFeatureProvider)
    {
        this.backendApiClientProvider = backendApiClientProvider;
        this.identityGenerator = identityGenerator;
        this.workflowRootActivityTemplateProvider = workflowRootActivityTemplateProvider;
        this.mediator = mediator;
        this.remoteFeatureProvider = remoteFeatureProvider;
    }

    /// <inheritdoc />
    public async Task<PagedListResponse<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        return await api.ListAsync(request, versionOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default)
    {
        var backendContextKey = backendApiClientProvider.ContextKey;
        var api = await GetApiAsync(cancellationToken);
        EnsureSameBackendContext(backendContextKey, cancellationToken);
        var definition = await api.GetByDefinitionIdAsync(definitionId, versionOptions, cancellationToken);
        BindReadDefinition(definition, backendContextKey, cancellationToken);
        return definition;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var backendContextKey = backendApiClientProvider.ContextKey;
        var api = await GetApiAsync(cancellationToken);
        EnsureSameBackendContext(backendContextKey, cancellationToken);
        var definition = await api.GetByIdAsync(id, cancellationToken);
        BindReadDefinition(definition, backendContextKey, cancellationToken);
        return definition;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var backendContextKey = backendApiClientProvider.ContextKey;
        var api = await GetApiAsync(cancellationToken);
        EnsureSameBackendContext(backendContextKey, cancellationToken);
        var response = await api.GetManyByIdAsync(ids.ToList(), cancellationToken);
        EnsureSameBackendContext(backendContextKey, cancellationToken);
        foreach (var definition in response.Items)
            BindReadDefinition(definition, backendContextKey, cancellationToken);
        return response.Items;
    }

    /// <inheritdoc />
    public async Task<ActivityNode?> FindSubgraphAsync(string id, string? parentNodeId = null, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        return await api.GetSubgraphAsync(id, parentNodeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GetPathSegmentsResponse?> GetPathSegmentsAsync(string id, string? childNodeId = null, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        return await api.GetPathSegmentsGetAsync(id, childNodeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SaveWorkflowDefinitionResponse> PublishAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var backendContextKey = backendApiClientProvider.ContextKey;
        var requestContextBinding = backendApiClientProvider as IBackendRequestContextBinding;
        var requiresRequestContextBinding = requestContextBinding?.SupportsAuthenticatedRequestContextBinding == true;
        var api = await GetApiAsync(cancellationToken);
        EnsureSameBackendContext(backendContextKey, cancellationToken);
        var supportsNativeCompensationDrafts = await NativeCompensationDraftCapability.IsEnabledForContextAsync(
            remoteFeatureProvider, backendApiClientProvider, backendContextKey, cancellationToken);
        requiresRequestContextBinding |= supportsNativeCompensationDrafts || requestContextBinding?.SupportsAuthenticatedRequestContextBinding == true;
        if (!supportsNativeCompensationDrafts)
        {
            WorkflowDefinition? definition;
            if (requiresRequestContextBinding)
            {
                using (requestContextBinding!.BindRequestContext(backendContextKey))
                    definition = await api.GetByDefinitionIdAsync(definitionId, VersionOptions.Latest, cancellationToken);
            }
            else
                definition = await api.GetByDefinitionIdAsync(definitionId, VersionOptions.Latest, cancellationToken);

            EnsureSameBackendContext(backendContextKey, cancellationToken);
            if (definition != null && NativeCompensationDraftCapability.ContainsCompensationScope(definition.Root))
                throw new NativeCompensationDraftPublishUnavailableException("This server does not advertise native compensation draft authoring. The workflow was not published.");
        }

        EnsureSameBackendContext(backendContextKey, cancellationToken);
        await mediator.NotifyAsync(new WorkflowDefinitionIdPublishing(definitionId), cancellationToken);
        EnsureSameBackendContext(backendContextKey, cancellationToken);
        SaveWorkflowDefinitionResponse response;
        requiresRequestContextBinding |= requestContextBinding?.SupportsAuthenticatedRequestContextBinding == true;
        if (requiresRequestContextBinding)
        {
            using (requestContextBinding!.BindRequestContext(backendContextKey))
                response = await api.PublishAsync(definitionId, new(), cancellationToken);
        }
        else
            response = await api.PublishAsync(definitionId, new(), cancellationToken);

        EnsureSameBackendContext(backendContextKey, cancellationToken);
        await mediator.NotifyAsync(new WorkflowDefinitionPublished(response.WorkflowDefinition), cancellationToken);
        return response;
    }

    /// <inheritdoc />
    public async Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var api = await GetApiAsync(cancellationToken);
            await mediator.NotifyAsync(new WorkflowDefinitionIdRetracting(definitionId), cancellationToken);
            var definition = await api.RetractAsync(definitionId, new(), cancellationToken);
            await mediator.NotifyAsync(new WorkflowDefinitionRetracted(definition), cancellationToken);
            return new(definition);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.BadRequest)
        {
            var errors = e.GetValidationErrors();
            await mediator.NotifyAsync(new WorkflowDefinitionIdRetractingFailed(definitionId, errors), cancellationToken);
            return new(errors);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);

        try
        {
            await mediator.NotifyAsync(new WorkflowDefinitionDeleting(definitionId), cancellationToken);
            await api.DeleteAsync(definitionId, cancellationToken);
            await mediator.NotifyAsync(new WorkflowDefinitionDeleted(definitionId), cancellationToken);
            return true;
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteVersionAsync(WorkflowDefinitionVersion workflowDefinitionVersion, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);

        try
        {
            await mediator.NotifyAsync(new WorkflowDefinitionVersionDeleting(workflowDefinitionVersion), cancellationToken);
            await api.DeleteVersionAsync(workflowDefinitionVersion.WorkflowDefinitionVersionId, cancellationToken);
            await mediator.NotifyAsync(new WorkflowDefinitionVersionDeleted(workflowDefinitionVersion), cancellationToken);
            return true;
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<long> BulkDeleteAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.ToList();
        await mediator.NotifyAsync(new BulkWorkflowDefinitionsDeleting(definitionIdList), cancellationToken);
        var request = new BulkDeleteWorkflowDefinitionsRequest(definitionIdList);
        var api = await GetApiAsync(cancellationToken);
        var response = await api.BulkDeleteAsync(request, cancellationToken);
        await mediator.NotifyAsync(new BulkWorkflowDefinitionsDeleted(definitionIdList), cancellationToken);
        return response.Deleted;
    }

    /// <inheritdoc />
    public async Task<long> BulkDeleteVersionsAsync(IEnumerable<WorkflowDefinitionVersion> workflowDefinitionVersions, CancellationToken cancellationToken = default)
    {
        var versions = workflowDefinitionVersions.ToList();
        var ids = versions.Select(x => x.WorkflowDefinitionVersionId).ToList();
        await mediator.NotifyAsync(new BulkWorkflowDefinitionVersionsDeleting(versions), cancellationToken);
        var request = new BulkDeleteWorkflowDefinitionVersionsRequest(ids);
        var api = await GetApiAsync(cancellationToken);
        var response = await api.BulkDeleteVersionsAsync(request, cancellationToken);
        await mediator.NotifyAsync(new BulkWorkflowDefinitionVersionsDeleted(versions), cancellationToken);
        return response.Deleted;
    }

    /// <inheritdoc />
    public async Task<BulkPublishWorkflowDefinitionsResponse> BulkPublishAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var backendContextKey = backendApiClientProvider.ContextKey;
        var requestContextBinding = backendApiClientProvider as IBackendRequestContextBinding;
        var requiresRequestContextBinding = requestContextBinding?.SupportsAuthenticatedRequestContextBinding == true;
        var definitionIdList = definitionIds.ToList();
        var api = await GetApiAsync(cancellationToken);
        EnsureSameBackendContext(backendContextKey, cancellationToken);
        var supportsNativeCompensationDrafts = await NativeCompensationDraftCapability.IsEnabledForContextAsync(
            remoteFeatureProvider, backendApiClientProvider, backendContextKey, cancellationToken);
        requiresRequestContextBinding |= supportsNativeCompensationDrafts || requestContextBinding?.SupportsAuthenticatedRequestContextBinding == true;
        if (!supportsNativeCompensationDrafts)
        {
            ListResponse<WorkflowDefinition> definitions;
            if (requiresRequestContextBinding)
            {
                using (requestContextBinding!.BindRequestContext(backendContextKey))
                    definitions = await api.GetManyByIdAsync(definitionIdList, cancellationToken);
            }
            else
                definitions = await api.GetManyByIdAsync(definitionIdList, cancellationToken);

            EnsureSameBackendContext(backendContextKey, cancellationToken);
            if (definitions.Items.Any(x => NativeCompensationDraftCapability.ContainsCompensationScope(x.Root)))
                throw new NativeCompensationDraftPublishUnavailableException("This server does not advertise native compensation draft authoring. The selected workflows were not published.");
        }

        var request = new BulkPublishWorkflowDefinitionsRequest(definitionIdList);
        EnsureSameBackendContext(backendContextKey, cancellationToken);
        await mediator.NotifyAsync(new BulkWorkflowDefinitionsPublishing(request.DefinitionIds), cancellationToken);
        EnsureSameBackendContext(backendContextKey, cancellationToken);
        BulkPublishWorkflowDefinitionsResponse response;
        requiresRequestContextBinding |= requestContextBinding?.SupportsAuthenticatedRequestContextBinding == true;
        if (requiresRequestContextBinding)
        {
            using (requestContextBinding!.BindRequestContext(backendContextKey))
                response = await api.BulkPublishAsync(request, cancellationToken);
        }
        else
            response = await api.BulkPublishAsync(request, cancellationToken);

        EnsureSameBackendContext(backendContextKey, cancellationToken);
        await mediator.NotifyAsync(new BulkWorkflowDefinitionsPublished(response.Published), cancellationToken);
        return response;
    }

    /// <inheritdoc />
    public async Task<BulkRetractWorkflowDefinitionsResponse> BulkRetractAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.ToList();
        await mediator.NotifyAsync(new BulkWorkflowDefinitionsRetracting(definitionIdList), cancellationToken);
        var request = new BulkRetractWorkflowDefinitionsRequest(definitionIdList);
        var api = await GetApiAsync(cancellationToken);
        var response = await api.BulkRetractAsync(request, cancellationToken);
        await mediator.NotifyAsync(new BulkWorkflowDefinitionsRetracted(response.Retracted), cancellationToken);
        return response;
    }

    /// <inheritdoc />
    public async Task<bool> GetIsNameUniqueAsync(string name, string? definitionId = null, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var response = await api.GetIsNameUniqueAsync(name, definitionId, cancellationToken);

        return response.IsUnique;
    }

    /// <inheritdoc />
    public async Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 100;
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            var name = $"Workflow {++attempt}";
            var isUnique = await GetIsNameUniqueAsync(name, null, cancellationToken);

            if (isUnique)
                return name;
        }

        throw new($"Failed to generate a unique workflow name after {maxAttempts} attempts.");
    }

    /// <inheritdoc />
    public async Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(
        string name,
        string? description = null,
        Action<SaveWorkflowDefinitionRequest>? configureRequest = null,
        CancellationToken cancellationToken = default)
    {
        return await CreateNewDefinitionAsync(name, description, null, configureRequest, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(
        string name,
        string? description,
        string? rootActivityTemplateKey,
        Action<SaveWorkflowDefinitionRequest>? configureRequest = null,
        CancellationToken cancellationToken = default)
    {
        var backendContextKey = backendApiClientProvider.ContextKey;
        var rootActivityTemplate = workflowRootActivityTemplateProvider.Find(rootActivityTemplateKey) ?? workflowRootActivityTemplateProvider.GetDefault();
        var saveRequest = new SaveWorkflowDefinitionRequest
        {
            Model = new()
            {
                Name = name,
                Description = description,
                Version = 1,
                ToolVersion = ToolVersion.Version,
                IsLatest = true,
                IsPublished = false,
                Root = rootActivityTemplate.CreateRoot(identityGenerator)
            }
        };

        configureRequest?.Invoke(saveRequest);

        var request = new SaveWorkflowDefinitionRequest
        {
            Model = WorkflowDefinitionModelSnapshot.WithRoot(saveRequest.Model, saveRequest.Model.Root),
            Publish = saveRequest.Publish
        };
        var containsCompensationDraft = NativeCompensationDraftCapability.ContainsCompensationScope(request.Model.Root);
        if (containsCompensationDraft &&
            !await NativeCompensationDraftCapability.IsEnabledForContextAsync(remoteFeatureProvider, backendApiClientProvider, backendContextKey, cancellationToken))
            return new(new ValidationErrors([new("This server does not advertise native compensation draft authoring. The workflow was not saved.")]));

        var api = await GetApiAsync(cancellationToken);
        if (containsCompensationDraft)
            EnsureSameBackendContext(backendContextKey, cancellationToken);

        try
        {
            if (containsCompensationDraft)
                EnsureSameBackendContext(backendContextKey, cancellationToken);
            SaveWorkflowDefinitionResponse response;
            if (containsCompensationDraft)
            {
                var binding = (IBackendRequestContextBinding)backendApiClientProvider;
                using (binding.BindRequestContext(backendContextKey))
                    response = await api.SaveAsync(request, cancellationToken);
            }
            else
                response = await api.SaveAsync(request, cancellationToken);

            if (containsCompensationDraft)
                EnsureSameBackendContext(backendContextKey, cancellationToken);
            NativeCompensationDraftDocumentContext.TryBind(response.WorkflowDefinition, backendContextKey);
            return new(response.WorkflowDefinition);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.BadRequest)
        {
            var errors = NativeCompensationDraftRefusal.TryGetValidationErrorsFromContent(e.Content, e.StatusCode) ?? e.GetValidationErrors();
            return new(errors);
        }
    }

    /// <inheritdoc />
    public async Task<FileDownload> ExportDefinitionAsync(string definitionId, VersionOptions? versionOptions = null, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        await mediator.NotifyAsync(new WorkflowDefinitionIdExporting(definitionId, versionOptions), cancellationToken);
        var response = await api.ExportAsync(definitionId, versionOptions, includeConsumingWorkflows, cancellationToken);
        var fileName = response.GetDownloadedFileNameOrDefault($"workflow-definition-{definitionId}.json");
        var fileDownload = new FileDownload(fileName, response.Content!);
        await mediator.NotifyAsync(new WorkflowDefinitionIdExported(definitionId, versionOptions, fileDownload), cancellationToken);

        return fileDownload;
    }

    /// <inheritdoc />
    public async Task<FileDownload> BulkExportDefinitionsAsync(IEnumerable<string> ids, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var request = new BulkExportWorkflowDefinitionsRequest(ids.ToArray(), includeConsumingWorkflows);
        var response = await api.BulkExportAsync(request, cancellationToken);
        var fileName = response.GetDownloadedFileNameOrDefault("workflow-definitions.zip");
        return new(fileName, response.Content!);
    }

    /// <inheritdoc />
    public async Task<UpdateConsumingWorkflowReferencesResponse> UpdateReferencesAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        return await api.UpdateReferencesAsync(definitionId, new(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ExecuteWorkflowResult> ExecuteAsync(string definitionId, ExecuteWorkflowDefinitionRequest? request, CancellationToken cancellationToken = default)
    {
        var api = await GetExecuteWorkflowApiAsync(cancellationToken);
        var response = await api.ExecuteAsync(definitionId, request, cancellationToken);
        var workflowInstanceId = response.Headers.TryGetValues("x-elsa-workflow-instance-id", out var workflowInstanceIdValues) ? workflowInstanceIdValues.FirstOrDefault() : null;
        var cannotStart = string.Equals(response.Headers.TryGetValues("x-elsa-workflow-cannot-start", out var cannotStartValues) ? cannotStartValues.FirstOrDefault() : null, "true", StringComparison.OrdinalIgnoreCase);
        return new(workflowInstanceId, cannotStart);
    }

    private async Task<IWorkflowDefinitionsApi> GetApiAsync(CancellationToken cancellationToken = default)
    {
        return await backendApiClientProvider.GetApiAsync<IWorkflowDefinitionsApi>(cancellationToken);
    }

    private async Task<IExecuteWorkflowApi> GetExecuteWorkflowApiAsync(CancellationToken cancellationToken = default)
    {
        return await backendApiClientProvider.GetApiAsync<IExecuteWorkflowApi>(cancellationToken);
    }

    private void BindReadDefinition(WorkflowDefinition? definition, string backendContextKey, CancellationToken cancellationToken)
    {
        EnsureSameBackendContext(backendContextKey, cancellationToken);
        if (definition != null && !NativeCompensationDraftDocumentContext.TryBind(definition, backendContextKey))
            throw new OperationCanceledException("The workflow definition was already bound to a different backend context.", cancellationToken);
    }

    private void EnsureSameBackendContext(string expected, CancellationToken cancellationToken)
    {
        if (!string.Equals(backendApiClientProvider.ContextKey, expected, StringComparison.Ordinal))
            throw new OperationCanceledException("The selected backend changed while loading or saving the workflow definition.", cancellationToken);
    }
}
