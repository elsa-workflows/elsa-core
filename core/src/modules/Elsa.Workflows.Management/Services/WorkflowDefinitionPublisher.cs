using Elsa.Common;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionPublisher(
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowDefinitionStore workflowDefinitionStore,
    IWorkflowValidator workflowValidator,
    IIdentityGenerator identityGenerator,
    IActivitySerializer activitySerializer,
    IMediator mediator,
    ISystemClock systemClock,
    IOptions<ManagementOptions> options)
    : IWorkflowDefinitionPublisher
{
    /// <inheritdoc />
    public async Task<PublishWorkflowDefinitionResult> CreateInitialAsync(WorkflowDefinition definition, bool publish, CancellationToken cancellationToken = default)
    {
        definition = Initialize(definition);
        definition.Version = 1;
        definition.IsLatest = true;
        definition.IsPublished = publish;
        var validationErrors = new List<WorkflowValidationError>();
        var affectedWorkflows = new AffectedWorkflows(new List<WorkflowDefinition>());

        if (publish)
        {
            validationErrors = await ValidateForPublicationAsync(definition, cancellationToken);
            if (options.Value.FailOnValidationErrors && validationErrors.Any(error => !error.IsWarning))
                return new(false, validationErrors, affectedWorkflows);

            await mediator.SendAsync(new WorkflowDefinitionPublishing(definition), cancellationToken);
        }
        else
        {
            await mediator.SendAsync(new WorkflowDefinitionDraftSaving(definition), cancellationToken);
        }

        if (!await workflowDefinitionStore.TryAddAsync(definition, cancellationToken))
            throw new WorkflowDefinitionAlreadyExistsException();

        if (publish)
        {
            await mediator.SendAsync(new WorkflowDefinitionPublished(definition, affectedWorkflows), cancellationToken);
        }
        else
        {
            await mediator.SendAsync(new WorkflowDefinitionDraftSaved(definition), cancellationToken);
            await mediator.SendAsync(new WorkflowDefinitionCreated(definition), cancellationToken);
        }

        return new(true, validationErrors, affectedWorkflows);
    }

    /// <inheritdoc />
    public async Task<PublishWorkflowDefinitionResult> PublishAsync(WorkflowDefinition definition,
        IReadOnlyCollection<WorkflowDefinition> expectedVersions, CancellationToken cancellationToken)
    {
        if (expectedVersions.Count == 0 || string.IsNullOrWhiteSpace(definition.Id) || string.IsNullOrWhiteSpace(definition.DefinitionId) || definition.Version <= 0)
            throw new WorkflowDefinitionEditConflictException();

        var expected = expectedVersions.ToDictionary(version => version.Id, version => activitySerializer.Serialize(version), StringComparer.Ordinal);
        var validationErrors = await ValidateForPublicationAsync(definition, cancellationToken);
        if (options.Value.FailOnValidationErrors && validationErrors.Any(error => !error.IsWarning))
            return new(false, validationErrors, new([]));

        await mediator.SendAsync(new WorkflowDefinitionPublishing(definition), cancellationToken);
        var proposed = definition.ShallowClone();
        proposed.IsPublished = true;
        proposed.IsLatest = true;

        if (!await workflowDefinitionStore.TryPublishAsync(proposed, MatchesExpected, cancellationToken))
            throw new WorkflowDefinitionEditConflictException();

        foreach (var previous in expectedVersions.Where(version => version.IsPublished && version.Id != proposed.Id))
        {
            var retracted = previous.ShallowClone();
            retracted.IsPublished = false;
            retracted.IsLatest = false;
            await mediator.SendAsync(new WorkflowDefinitionVersionRetracted(retracted), cancellationToken);
        }

        definition.IsPublished = true;
        definition.IsLatest = true;
        definition.TenantId = proposed.TenantId;
        var affectedWorkflows = new AffectedWorkflows(new List<WorkflowDefinition>());
        await mediator.SendAsync(new WorkflowDefinitionPublished(definition, affectedWorkflows), cancellationToken);
        return new(true, validationErrors, affectedWorkflows);

        bool MatchesExpected(IReadOnlyCollection<WorkflowDefinition> versions) =>
            !options.Value.IsReadOnlyMode && versions.Count == expected.Count && versions.All(version =>
                !version.IsReadonly && !version.IsSystem && expected.TryGetValue(version.Id, out var value) &&
                string.Equals(value, activitySerializer.Serialize(version), StringComparison.Ordinal));
    }

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
        var validationErrors = await ValidateForPublicationAsync(definition, cancellationToken);

        if (options.Value.FailOnValidationErrors && validationErrors.Any(error => !error.IsWarning))
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

        var expected = definition.ShallowClone();
        var retracted = expected.ShallowClone();
        retracted.IsPublished = false;
        await mediator.SendAsync(new WorkflowDefinitionRetracting(retracted), cancellationToken);

        if (!await workflowDefinitionStore.TryRetractAsync(expected, cancellationToken))
            throw new WorkflowDefinitionEditConflictException();

        definition.IsPublished = false;
        await mediator.SendAsync(new WorkflowDefinitionRetracted(definition), cancellationToken);
        return definition;
    }

    public async Task<WorkflowDefinition> RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        var latestVersionFilter = new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId,
            VersionOptions = VersionOptions.Latest
        };
        var latestVersion = await workflowDefinitionStore.FindAsync(latestVersionFilter, cancellationToken);
        var lastVersionFilter = new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId
        };
        var lastVersion = await workflowDefinitionStore.FindLastVersionAsync(lastVersionFilter, cancellationToken);

        if (latestVersion != null)
        {
            latestVersion.IsLatest = false;
            await workflowDefinitionStore.SaveAsync(latestVersion, cancellationToken);
        }

        var draft = await GetDraftAsync(definitionId, VersionOptions.SpecificVersion(version), cancellationToken);
        draft!.Id = identityGenerator.GenerateId();
        draft.Version = (lastVersion?.Version ?? 0) + 1;
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

        var draft = definition.ShallowClone();

        if (!definition.IsPublished)
            return draft;

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
        var highestVersion = await workflowDefinitionStore.FindLastVersionAsync(filter, cancellationToken);
        var latestVersion = await workflowDefinitionStore.FindAsync(new()
        {
            DefinitionId = definitionId,
            VersionOptions = VersionOptions.Latest
        }, cancellationToken);

        if (latestVersion == null)
        {
            if (highestVersion != null)
                throw new WorkflowDefinitionEditConflictException();

            await CreateInitialAsync(draft, publish: false, cancellationToken);
            return draft;
        }

        if (options.Value.IsReadOnlyMode || latestVersion.IsReadonly || latestVersion.IsSystem)
            throw new WorkflowDefinitionEditConflictException();

        var expectedId = latestVersion.Id;
        var snapshot = activitySerializer.Serialize(latestVersion);
        WorkflowDefinition? existingDraft = null;
        if (!string.IsNullOrEmpty(draft.Id) && draft.Id != expectedId)
        {
            existingDraft = await workflowDefinitionStore.FindAsync(new()
            {
                Id = draft.Id,
                DefinitionId = definitionId
            }, cancellationToken);
        }

        if (existingDraft != null)
        {
            return await PromoteExistingDraftAsync(draft, existingDraft, latestVersion, snapshot, cancellationToken);
        }

        draft.Version = draft.Id == expectedId ? latestVersion.Version : (highestVersion?.Version ?? latestVersion.Version) + 1;
        draft.IsLatest = true;
        draft = Initialize(draft);
        await mediator.SendAsync(new WorkflowDefinitionDraftSaving(draft), cancellationToken);

        var result = await workflowDefinitionStore.TryUpdateLatestAsync(
            new WorkflowDefinitionFilter { Id = expectedId, DefinitionId = definitionId },
            current => !options.Value.IsReadOnlyMode && current.IsLatest && !current.IsReadonly && !current.IsSystem &&
                string.Equals(snapshot, activitySerializer.Serialize(current), StringComparison.Ordinal),
            _ => draft, cancellationToken);
        if (result.Outcome != WorkflowDefinitionUpdateOutcome.Updated)
            throw new WorkflowDefinitionEditConflictException();

        await mediator.SendAsync(new WorkflowDefinitionDraftSaved(draft), cancellationToken);
        return draft;
    }

    private async Task<WorkflowDefinition> PromoteExistingDraftAsync(WorkflowDefinition draft, WorkflowDefinition existingDraft,
        WorkflowDefinition latestVersion, string latestSnapshot, CancellationToken cancellationToken)
    {
        if (existingDraft.IsLatest || existingDraft.IsPublished || existingDraft.IsReadonly || existingDraft.IsSystem ||
            existingDraft.TenantId != latestVersion.TenantId)
        {
            throw new WorkflowDefinitionEditConflictException();
        }

        var draftSnapshot = activitySerializer.Serialize(existingDraft);
        var expected = new Dictionary<string, string>
        {
            [existingDraft.Id] = draftSnapshot,
            [latestVersion.Id] = latestSnapshot
        };

        draft.Version = existingDraft.Version;
        draft.TenantId = existingDraft.TenantId;
        draft.IsLatest = true;
        draft.IsPublished = false;
        await mediator.SendAsync(new WorkflowDefinitionDraftSaving(draft), cancellationToken);

        if (!await workflowDefinitionStore.TryPromoteExistingDraftAsync(draft, MatchesExpected, cancellationToken))
        {
            throw new WorkflowDefinitionEditConflictException();
        }

        await mediator.SendAsync(new WorkflowDefinitionDraftSaved(draft), cancellationToken);
        return draft;

        bool MatchesExpected(IReadOnlyCollection<WorkflowDefinition> versions) =>
            !options.Value.IsReadOnlyMode && versions.Count == expected.Count && versions.All(version =>
                !version.IsReadonly && !version.IsSystem && expected.TryGetValue(version.Id, out var value) &&
                string.Equals(value, activitySerializer.Serialize(version), StringComparison.Ordinal));
    }

    private async Task<List<WorkflowValidationError>> ValidateForPublicationAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        var graph = await workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        return (await workflowValidator.ValidateAsync(graph.Workflow, cancellationToken)).ToList();
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
