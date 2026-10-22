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
public class WorkflowDefinitionEditorService : IWorkflowDefinitionEditorService, IContextBoundWorkflowDefinitionEditorService
{
    private readonly IBackendApiClientProvider backendApiClientProvider;
    private readonly IMediator mediator;
    private readonly IRemoteFeatureProvider? remoteFeatureProvider;
    private readonly INativeCompensationDraftGraphClassifier nativeDraftGraphClassifier;

    public WorkflowDefinitionEditorService(IBackendApiClientProvider backendApiClientProvider, IMediator mediator)
        : this(backendApiClientProvider, mediator, null, new NativeCompensationDraftGraphClassifier())
    {
    }

    public WorkflowDefinitionEditorService(
        IBackendApiClientProvider backendApiClientProvider,
        IMediator mediator,
        IRemoteFeatureProvider? remoteFeatureProvider)
        : this(backendApiClientProvider, mediator, remoteFeatureProvider, new NativeCompensationDraftGraphClassifier())
    {
    }

    public WorkflowDefinitionEditorService(
        IBackendApiClientProvider backendApiClientProvider,
        IMediator mediator,
        IRemoteFeatureProvider? remoteFeatureProvider,
        INativeCompensationDraftGraphClassifier nativeDraftGraphClassifier)
    {
        this.backendApiClientProvider = backendApiClientProvider;
        this.mediator = mediator;
        this.remoteFeatureProvider = remoteFeatureProvider;
        this.nativeDraftGraphClassifier = nativeDraftGraphClassifier;
    }

    /// <inheritdoc />
    public Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveAsync(WorkflowDefinition workflowDefinition, bool publish, Func<WorkflowDefinition, Task>? workflowSavedCallback = null, CancellationToken cancellationToken = default) =>
        SaveCoreAsync(workflowDefinition, publish, null, workflowSavedCallback, cancellationToken);

    /// <inheritdoc />
    public Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveNativeCompensationDraftAsync(
        WorkflowDefinition workflowDefinition,
        bool publish,
        string expectedBackendContextKey,
        Func<WorkflowDefinition, Task>? workflowSavedCallback = null,
        CancellationToken cancellationToken = default) =>
        SaveCoreAsync(workflowDefinition, publish, expectedBackendContextKey, workflowSavedCallback, cancellationToken);

    private async Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveCoreAsync(
        WorkflowDefinition workflowDefinition,
        bool publish,
        string? expectedBackendContextKey,
        Func<WorkflowDefinition, Task>? workflowSavedCallback,
        CancellationToken cancellationToken)
    {
        // Classify and send one owned snapshot. The editor root is mutable JSON and may change while
        // API acquisition or mediator callbacks are awaiting; the request must not inherit unchecked edits.
        var rootSnapshot = workflowDefinition.Root?.DeepClone().AsObject();
        var analysis = await nativeDraftGraphClassifier.AnalyzeAsync(rootSnapshot, cancellationToken);
        if (!analysis.IsComplete && analysis.HasPotentialScopeMarker)
            return new(new ValidationErrors([new("This workflow contains a native compensation scope Studio cannot safely classify or edit. The workflow was not saved.")]));
        var containsCompensationDraft = analysis.ContainsCompensationScope;
        if (containsCompensationDraft && analysis.ContainsUnsupportedScope)
            return new(new ValidationErrors([new("This workflow contains a native compensation scope Studio cannot safely edit. The workflow was not saved.")]));

        var draftOriginContextKey = containsCompensationDraft
            ? NativeCompensationDraftDocumentContext.GetBackendContextKey(workflowDefinition)
            : null;
        if (containsCompensationDraft &&
            (draftOriginContextKey == null ||
             expectedBackendContextKey != null && expectedBackendContextKey != draftOriginContextKey ||
             backendApiClientProvider.ContextKey != draftOriginContextKey))
            return DraftContextChanged();

        if (containsCompensationDraft &&
            !await NativeCompensationDraftCapability.IsEnabledForContextAsync(remoteFeatureProvider, backendApiClientProvider, draftOriginContextKey!, cancellationToken))
            return backendApiClientProvider.ContextKey == draftOriginContextKey
                ? new(new ValidationErrors([new("This connection cannot verify safe native compensation draft authoring. A fresh backend feature check and supported authenticated request binding are required; the workflow was not saved.")]))
                : DraftContextChanged();

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
                Root = rootSnapshot
            },
            Publish = publish,
        };

        var api = await GetApiAsync(cancellationToken);
        if (containsCompensationDraft && backendApiClientProvider.ContextKey != draftOriginContextKey)
            return DraftContextChanged();

        try
        {
            if (request.Publish == true)
            {
                await mediator.NotifyAsync(new WorkflowDefinitionPublishing(workflowDefinition), cancellationToken);
                if (containsCompensationDraft && backendApiClientProvider.ContextKey != draftOriginContextKey)
                    return DraftContextChanged();
            }
            await mediator.NotifyAsync(new WorkflowDefinitionSaving(workflowDefinition), cancellationToken);
            if (containsCompensationDraft && backendApiClientProvider.ContextKey != draftOriginContextKey)
                return DraftContextChanged();

            IDisposable? requestContextScope = null;
            if (containsCompensationDraft)
            {
                if (backendApiClientProvider is not IBackendRequestContextBinding requestContextBinding)
                    return DraftContextChanged();

                try
                {
                    requestContextScope = requestContextBinding.BindRequestContext(draftOriginContextKey!);
                }
                catch (BackendRequestContextChangedException)
                {
                    return DraftContextChanged();
                }
            }

            SaveWorkflowDefinitionResponse response;
            try
            {
                using (requestContextScope)
                    response = await api.SaveAsync(request, cancellationToken);
            }
            catch (BackendRequestContextChangedException)
            {
                return DraftContextChanged();
            }

            if (containsCompensationDraft)
            {
                if (backendApiClientProvider.ContextKey != draftOriginContextKey)
                    return DraftContextChanged();
                if (!NativeCompensationDraftDocumentContext.TryBind(response.WorkflowDefinition, draftOriginContextKey!))
                    return DraftContextChanged();
            }
            else
            {
                NativeCompensationDraftDocumentContext.Copy(workflowDefinition, response.WorkflowDefinition);
            }

            if (workflowSavedCallback != null)
                await workflowSavedCallback(response.WorkflowDefinition);

            if (containsCompensationDraft && backendApiClientProvider.ContextKey != draftOriginContextKey)
                return DraftContextChanged();

            await mediator.NotifyAsync(new WorkflowDefinitionSaved(response.WorkflowDefinition), cancellationToken);
            if (request.Publish == true) await mediator.NotifyAsync(new WorkflowDefinitionPublished(response.WorkflowDefinition), cancellationToken);
            return new(response);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.BadRequest)
        {
            var errors = NativeCompensationDraftRefusal.TryGetValidationErrorsFromContent(e.Content, e.StatusCode)
                         ?? e.GetValidationErrors();
            await mediator.NotifyAsync(new WorkflowDefinitionSavingFailed(workflowDefinition, errors), cancellationToken);
            if (request.Publish == true) await mediator.NotifyAsync(new WorkflowDefinitionPublishingFailed(workflowDefinition, errors), cancellationToken);
            return new(errors);
        }
        catch (BackendRequestContextChangedException) when (containsCompensationDraft)
        {
            return DraftContextChanged();
        }

        static Result<SaveWorkflowDefinitionResponse, ValidationErrors> DraftContextChanged() =>
            new(new ValidationErrors([new("This compensation draft was loaded from a different backend context. Reload the workflow before saving.")]));
    }

    /// <inheritdoc />
    public async Task<SaveWorkflowDefinitionResponse> PublishAsync(WorkflowDefinition workflowDefinition, Func<WorkflowDefinition, Task>? workflowPublishedCallback = null, CancellationToken cancellationToken = default)
    {
        var rootSnapshot = workflowDefinition.Root?.DeepClone().AsObject();
        var analysis = await nativeDraftGraphClassifier.AnalyzeAsync(rootSnapshot, cancellationToken);
        if (!analysis.IsComplete && analysis.HasPotentialScopeMarker)
            throw new InvalidOperationException("This workflow contains a native compensation scope Studio cannot safely classify or publish.");
        var containsCompensationDraft = analysis.ContainsCompensationScope;
        var draftOriginContextKey = containsCompensationDraft
            ? NativeCompensationDraftDocumentContext.GetBackendContextKey(workflowDefinition)
            : null;
        if (containsCompensationDraft &&
            (draftOriginContextKey == null || backendApiClientProvider.ContextKey != draftOriginContextKey))
            throw new InvalidOperationException("This compensation draft was loaded from a different backend context. Reload the workflow before publishing.");

        if (containsCompensationDraft &&
            !await NativeCompensationDraftCapability.IsEnabledForContextAsync(remoteFeatureProvider, backendApiClientProvider, draftOriginContextKey!, cancellationToken))
            throw new InvalidOperationException(backendApiClientProvider.ContextKey == draftOriginContextKey
                ? "This connection cannot verify safe native compensation draft authoring. A fresh backend feature check and supported authenticated request binding are required; the workflow was not published."
                : "This compensation draft was loaded from a different backend context. Reload the workflow before publishing.");

        var api = await GetApiAsync(cancellationToken);
        if (containsCompensationDraft && backendApiClientProvider.ContextKey != draftOriginContextKey)
            throw new InvalidOperationException("This compensation draft was loaded from a different backend context. Reload the workflow before publishing.");
        await mediator.NotifyAsync(new WorkflowDefinitionPublishing(workflowDefinition), cancellationToken);
        if (containsCompensationDraft && backendApiClientProvider.ContextKey != draftOriginContextKey)
            throw new InvalidOperationException("This compensation draft was loaded from a different backend context. Reload the workflow before publishing.");

        IDisposable? requestContextScope = null;
        if (containsCompensationDraft)
        {
            if (backendApiClientProvider is not IBackendRequestContextBinding requestContextBinding)
                throw new InvalidOperationException("The authenticated backend connection cannot safely publish this compensation draft.");

            try
            {
                requestContextScope = requestContextBinding.BindRequestContext(draftOriginContextKey!);
            }
            catch (BackendRequestContextChangedException exception)
            {
                throw new InvalidOperationException("This compensation draft's backend identity changed before publishing. Reload the workflow before trying again.", exception);
            }
        }

        SaveWorkflowDefinitionResponse response;
        try
        {
            using (requestContextScope)
                response = await api.PublishAsync(workflowDefinition.DefinitionId, new PublishWorkflowDefinitionRequest(), cancellationToken);
        }
        catch (BackendRequestContextChangedException exception) when (containsCompensationDraft)
        {
            throw new InvalidOperationException("This compensation draft's backend identity changed before publishing. Reload the workflow before trying again.", exception);
        }

        if (containsCompensationDraft && backendApiClientProvider.ContextKey != draftOriginContextKey)
            throw new InvalidOperationException("This compensation draft was loaded from a different backend context. Reload the workflow before publishing.");
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
