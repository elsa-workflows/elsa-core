using Elsa.Common.Contracts;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionPublisher : IWorkflowDefinitionPublisher
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IActivitySerializer _activitySerializer;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowDefinitionPublisher(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IEventPublisher eventPublisher,
        IIdentityGenerator identityGenerator,
        IActivitySerializer activitySerializer,
        ISystemClock systemClock)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _eventPublisher = eventPublisher;
        _identityGenerator = identityGenerator;
        _activitySerializer = activitySerializer;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public WorkflowDefinition New()
    {
        var id = _identityGenerator.GenerateId();
        var definitionId = _identityGenerator.GenerateId();
        const int version = 1;

        return new WorkflowDefinition
        {
            Id = id,
            DefinitionId = definitionId,
            Version = version,
            IsLatest = true,
            IsPublished = false,
            CreatedAt = _systemClock.UtcNow,
            StringData = _activitySerializer.Serialize(new Sequence()),
            MaterializerName = JsonWorkflowMaterializer.MaterializerName
        };
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> PublishAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.Latest };
        var definition = await _workflowDefinitionStore.FindAsync(filter, cancellationToken);

        if (definition == null)
            return null;

        return await PublishAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition> PublishAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        await _eventPublisher.PublishAsync(new WorkflowDefinitionPublishing(definition), cancellationToken);

        var definitionId = definition.DefinitionId;

        // Reset current latest and published definitions.
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.LatestOrPublished };
        var publishedWorkflows = await _workflowDefinitionStore.FindManyAsync(filter, cancellationToken);

        foreach (var publishedAndOrLatestWorkflow in publishedWorkflows)
        {
            publishedAndOrLatestWorkflow.IsPublished = false;
            publishedAndOrLatestWorkflow.IsLatest = false;
            await _workflowDefinitionStore.SaveAsync(publishedAndOrLatestWorkflow, cancellationToken);
        }

        // Save the new published definition.
        definition.IsPublished = true;
        definition = Initialize(definition);
        await _workflowDefinitionStore.SaveAsync(definition, cancellationToken);

        await _eventPublisher.PublishAsync(new WorkflowDefinitionPublished(definition), cancellationToken);
        return definition;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> RetractAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.Published };
        var definition = await _workflowDefinitionStore.FindAsync(filter, cancellationToken);

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
        definition = Initialize(definition);

        await _eventPublisher.PublishAsync(new WorkflowDefinitionRetracting(definition), cancellationToken);
        await _workflowDefinitionStore.SaveAsync(definition, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowDefinitionRetracted(definition), cancellationToken);
        return definition;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> GetDraftAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = versionOptions };
        var order = new WorkflowDefinitionOrder<int>(x => x.Version, OrderDirection.Descending);
        var lastVersion = await _workflowDefinitionStore.FindLastVersionAsync(new WorkflowDefinitionFilter { DefinitionId = definitionId }, cancellationToken);
        var definition = (await _workflowDefinitionStore.FindAsync(filter, order, cancellationToken)) ?? lastVersion;

        if (definition == null!)
            return null;

        if (!definition.IsPublished)
            return definition;

        var draft = definition.ShallowClone();

        draft.Version = lastVersion?.Version + 1 ?? 1;
        draft.Id = _identityGenerator.GenerateId();
        draft.IsLatest = true;
        draft.IsPublished = false;

        return draft;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition> SaveDraftAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var draft = definition;
        var definitionId = definition.DefinitionId;
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId };
        var lastVersion = await _workflowDefinitionStore.FindLastVersionAsync(filter, cancellationToken);
        
        draft.Version = draft.Id == lastVersion?.Id ? lastVersion.Version : lastVersion?.Version + 1 ?? 1;
        draft.IsLatest = true;
        draft = Initialize(draft);

        await _workflowDefinitionStore.SaveAsync(draft, cancellationToken);

        if (lastVersion is null)
        {
            await _eventPublisher.PublishAsync(new WorkflowDefinitionCreated(definition), cancellationToken);
        }

        if (lastVersion is { IsPublished: true, IsLatest: true })
        {
            lastVersion.IsLatest = false;
            await _workflowDefinitionStore.SaveAsync(lastVersion, cancellationToken);
        }

        return draft;
    }

    private WorkflowDefinition Initialize(WorkflowDefinition definition)
    {
        if (definition.Id == null!)
            definition.Id = _identityGenerator.GenerateId();

        if (definition.DefinitionId == null!)
            definition.DefinitionId = _identityGenerator.GenerateId();

        if (definition.Version == 0)
            definition.Version = 1;

        return definition;
    }
}