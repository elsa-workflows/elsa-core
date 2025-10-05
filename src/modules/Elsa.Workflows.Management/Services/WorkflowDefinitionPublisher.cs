using Elsa.Common;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionPublisher(
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowDefinitionStore workflowDefinitionStore,
    IWorkflowValidator workflowValidator,
    IIdentityGenerator identityGenerator,
    IActivitySerializer activitySerializer,
    IMediator mediator,
    ISystemClock systemClock)
    : IWorkflowDefinitionPublisher
{
    /// <inheritdoc />
    public WorkflowDefinition New(IActivity? root = null)
    {
        root ??= new Sequence();
        var id = identityGenerator.GenerateId();
        var definitionId = identityGenerator.GenerateId();
        const int version = 1;

        return new()
        {
            Id = id,
            DefinitionId = definitionId,
            Version = version,
            IsLatest = true,
            IsPublished = false,
            CreatedAt = systemClock.UtcNow,
            StringData = activitySerializer.Serialize(root),
            MaterializerName = JsonWorkflowMaterializer.MaterializerName
        };
    }

    public Task<WorkflowDefinition> NewAsync(IActivity? root = null, CancellationToken cancellationToken = default)
    {
        root ??= new Sequence();
        var id = identityGenerator.GenerateId();
        var definitionId = identityGenerator.GenerateId();
        const int version = 1;

        var workflowDefinition = new WorkflowDefinition
        {
            Id = id,
            DefinitionId = definitionId,
            Version = version,
            IsLatest = true,
            IsPublished = false,
            CreatedAt = systemClock.UtcNow,
            StringData = activitySerializer.Serialize(root),
            MaterializerName = JsonWorkflowMaterializer.MaterializerName
        };

        return Task.FromResult(workflowDefinition);
    }

    /// <inheritdoc />
    public async Task<PublishWorkflowDefinitionResult> PublishAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await workflowDefinitionStore.FindAsync(filter, cancellationToken);

        if (definition == null)
            return new(false, new List<WorkflowValidationError>
            {
                new("Workflow definition not found.")
            }, new([]));

        return await PublishAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PublishWorkflowDefinitionResult> PublishAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var workflowGraph = await workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        var validationErrors = (await workflowValidator.ValidateAsync(workflowGraph.Workflow, cancellationToken)).ToList();

        if (validationErrors.Any())
            return new(false, validationErrors, new([]));

        await mediator.SendAsync(new WorkflowDefinitionPublishing(definition), cancellationToken);
        var definitionId = definition.DefinitionId;

        // Reset current latest and published definitions.
        var publishedWorkflows = await workflowDefinitionStore.FindManyAsync(new()
        {
            DefinitionId = definitionId,
            VersionOptions = VersionOptions.LatestOrPublished
        }, cancellationToken);

        foreach (var publishedAndOrLatestWorkflow in publishedWorkflows)
        {
            var isPublished = publishedAndOrLatestWorkflow.IsPublished;
            publishedAndOrLatestWorkflow.IsPublished = false;
            publishedAndOrLatestWorkflow.IsLatest = false;
            await workflowDefinitionStore.SaveAsync(publishedAndOrLatestWorkflow, cancellationToken);

            if (isPublished)
                await mediator.SendAsync(new WorkflowDefinitionVersionRetracted(publishedAndOrLatestWorkflow), cancellationToken);
        }

        // Save the newly published definition.
        definition.IsPublished = true;
        definition.IsLatest = true;
        definition = Initialize(definition);
        await workflowDefinitionStore.SaveAsync(definition, cancellationToken);

        var affectedWorkflows = new AffectedWorkflows(new List<WorkflowDefinition>());
        await mediator.SendAsync(new WorkflowDefinitionPublished(definition, affectedWorkflows), cancellationToken);
        return new(true, validationErrors, affectedWorkflows);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> RetractAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published).ToFilter();
        var definition = await workflowDefinitionStore.FindAsync(filter, cancellationToken);

        if (definition == null)
            return null;

        return await RetractAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition> RetractAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        if (!definition.IsPublished)
            throw new InvalidOperationException("Cannot retract an unpublished workflow definition.");

        definition.IsPublished = false;

        await mediator.SendAsync(new WorkflowDefinitionRetracting(definition), cancellationToken);
        await workflowDefinitionStore.SaveAsync(definition, cancellationToken);
        await mediator.SendAsync(new WorkflowDefinitionRetracted(definition), cancellationToken);
        return definition;
    }

    public async Task<WorkflowDefinition> RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId,
            VersionOptions = VersionOptions.Latest
        };
        var latestVersion = await workflowDefinitionStore.FindAsync(filter, cancellationToken);

        if (latestVersion != null)
        {
            latestVersion.IsLatest = false;
            await workflowDefinitionStore.SaveAsync(latestVersion, cancellationToken);
        }

        var draft = await GetDraftAsync(definitionId, VersionOptions.SpecificVersion(version), cancellationToken);
        draft!.Id = identityGenerator.GenerateId();
        draft.Version = (latestVersion?.Version ?? 0) + 1;
        draft.IsLatest = true;

        await workflowDefinitionStore.SaveAsync(draft, cancellationToken);
        return draft;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> GetDraftAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId,
            VersionOptions = versionOptions
        };
        var order = new WorkflowDefinitionOrder<int>(x => x.Version, OrderDirection.Descending);
        var lastVersion = await workflowDefinitionStore.FindLastVersionAsync(new()
        {
            DefinitionId = definitionId
        }, cancellationToken);
        var definition = await workflowDefinitionStore.FindAsync(filter, order, cancellationToken) ?? lastVersion;

        if (definition == null!)
            return null;

        if (!definition.IsPublished)
            return definition;

        var draft = definition.ShallowClone();

        draft.Version = lastVersion?.Version + 1 ?? 1;
        draft.CreatedAt = systemClock.UtcNow;
        draft.Id = identityGenerator.GenerateId();
        draft.IsLatest = true;
        draft.IsPublished = false;

        return draft;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition> SaveDraftAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var draft = definition;
        var definitionId = definition.DefinitionId;
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId
        };
        var lastVersion = await workflowDefinitionStore.FindLastVersionAsync(filter, cancellationToken);

        draft.Version = draft.Id == lastVersion?.Id ? lastVersion.Version : lastVersion?.Version + 1 ?? 1;
        draft.IsLatest = true;
        draft = Initialize(draft);

        await mediator.SendAsync(new WorkflowDefinitionDraftSaving(draft), cancellationToken);
        await workflowDefinitionStore.SaveAsync(draft, cancellationToken);
        await mediator.SendAsync(new WorkflowDefinitionDraftSaved(draft), cancellationToken);

        if (lastVersion is null)
            await mediator.SendAsync(new WorkflowDefinitionCreated(definition), cancellationToken);

        if (lastVersion is { IsPublished: true, IsLatest: true })
        {
            lastVersion.IsLatest = false;
            await workflowDefinitionStore.SaveAsync(lastVersion, cancellationToken);
        }

        return draft;
    }

    private WorkflowDefinition Initialize(WorkflowDefinition definition)
    {
        if (definition.Id == null!)
            definition.Id = identityGenerator.GenerateId();

        if (definition.DefinitionId == null!)
            definition.DefinitionId = identityGenerator.GenerateId();

        if (definition.Version == 0)
            definition.Version = 1;

        return definition;
    }
}