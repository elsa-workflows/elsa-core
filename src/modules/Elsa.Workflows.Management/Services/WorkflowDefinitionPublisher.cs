using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Framework.Entities;
using Elsa.Framework.System;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Management.Requests;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionPublisher(
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowDefinitionStore workflowDefinitionStore,
    INotificationSender notificationSender,
    IIdentityGenerator identityGenerator,
    IActivityVisitor activityVisitor,
    IActivitySerializer activitySerializer,
    IRequestSender requestSender,
    ISystemClock systemClock)
    : IWorkflowDefinitionPublisher
{
    /// <inheritdoc />
    public WorkflowDefinition New(IActivity? root = default)
    {
        root ??= new Sequence();
        var id = identityGenerator.GenerateId();
        var definitionId = identityGenerator.GenerateId();
        const int version = 1;

        return new WorkflowDefinition
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

    /// <inheritdoc />
    public async Task<PublishWorkflowDefinitionResult> PublishAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.Latest };
        var definition = await workflowDefinitionStore.FindAsync(filter, cancellationToken);

        if (definition == null)
            return new PublishWorkflowDefinitionResult(false, new List<WorkflowValidationError>
            {
                new("Workflow definition not found.")
            }, null);

        return await PublishAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PublishWorkflowDefinitionResult> PublishAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var workflowGraph = await workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        var responses = await requestSender.SendAsync(new ValidateWorkflowRequest(workflowGraph.Workflow), cancellationToken);
        var validationErrors = responses.SelectMany(r => r.ValidationErrors).ToList();

        if (validationErrors.Any())
            return new PublishWorkflowDefinitionResult(false, validationErrors, null);
        
        await notificationSender.SendAsync(new WorkflowDefinitionPublishing(definition), cancellationToken);

        var definitionId = definition.DefinitionId;

        // Reset current latest and published definitions.
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.LatestOrPublished };
        var publishedWorkflows = await workflowDefinitionStore.FindManyAsync(filter, cancellationToken);

        foreach (var publishedAndOrLatestWorkflow in publishedWorkflows)
        {
            publishedAndOrLatestWorkflow.IsPublished = false;
            publishedAndOrLatestWorkflow.IsLatest = false;
            await workflowDefinitionStore.SaveAsync(publishedAndOrLatestWorkflow, cancellationToken);
        }

        // Save the new published definition.
        definition.IsPublished = true;
        definition = Initialize(definition);
        await workflowDefinitionStore.SaveAsync(definition, cancellationToken);

        await notificationSender.SendAsync(new WorkflowDefinitionPublished(definition), cancellationToken);

        var consumingWorkflows = new List<WorkflowDefinition>();

        if (definition.Options.UsableAsActivity == true)
        {
            consumingWorkflows.AddRange(await UpdateReferencesInConsumingWorkflows(definition, cancellationToken));
        }
        
        return new PublishWorkflowDefinitionResult(true, validationErrors, consumingWorkflows);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> RetractAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.Published };
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

        await notificationSender.SendAsync(new WorkflowDefinitionRetracting(definition), cancellationToken);
        await workflowDefinitionStore.SaveAsync(definition, cancellationToken);
        await notificationSender.SendAsync(new WorkflowDefinitionRetracted(definition), cancellationToken);
        return definition;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> GetDraftAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = versionOptions };
        var order = new WorkflowDefinitionOrder<int>(x => x.Version, OrderDirection.Descending);
        var lastVersion = await workflowDefinitionStore.FindLastVersionAsync(new WorkflowDefinitionFilter { DefinitionId = definitionId }, cancellationToken);
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
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId };
        var lastVersion = await workflowDefinitionStore.FindLastVersionAsync(filter, cancellationToken);

        draft.Version = draft.Id == lastVersion?.Id ? lastVersion.Version : lastVersion?.Version + 1 ?? 1;
        draft.IsLatest = true;
        draft = Initialize(draft);

        await workflowDefinitionStore.SaveAsync(draft, cancellationToken);

        if (lastVersion is null)
        {
            await notificationSender.SendAsync(new WorkflowDefinitionCreated(definition), cancellationToken);
        }

        if (lastVersion is { IsPublished: true, IsLatest: true })
        {
            lastVersion.IsLatest = false;
            await workflowDefinitionStore.SaveAsync(lastVersion, cancellationToken);
        }

        return draft;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> UpdateReferencesInConsumingWorkflows(WorkflowDefinition dependency, CancellationToken cancellationToken = default)
    {
        var updatedWorkflowDefinitions = new List<WorkflowDefinition>();

        var workflowDefinitions = (await workflowDefinitionStore.FindManyAsync(new WorkflowDefinitionFilter
        {
            VersionOptions = VersionOptions.LatestOrPublished
        }, cancellationToken)).ToList();

        // Remove the dependency from the list of workflow definitions to consider.
        workflowDefinitions = workflowDefinitions.Where(x => x.DefinitionId != dependency.DefinitionId).ToList();

        foreach (var definition in workflowDefinitions)
        {
            var root = activitySerializer.Deserialize(definition.StringData!);
            var graph = await activityVisitor.VisitAsync(root, cancellationToken);
            var flattenedList = graph.Flatten().ToList();
            var definitionId = dependency.DefinitionId;
            var version = dependency.Version;
            var nodes = flattenedList
                .Where(x => x.Activity is WorkflowDefinitionActivity workflowDefinitionActivity && workflowDefinitionActivity.WorkflowDefinitionId == definitionId)
                .ToList();

            foreach (var node in nodes.Where(activity => activity.Activity.Version < version))
            {
                var activity = (WorkflowDefinitionActivity)node.Activity;
                activity.Version = version;
                activity.WorkflowDefinitionVersionId = dependency.Id;

                if (!updatedWorkflowDefinitions.Contains(definition))
                    updatedWorkflowDefinitions.Add(definition);
            }

            if (updatedWorkflowDefinitions.Contains(definition))
            {
                var serializedData = activitySerializer.Serialize(root);
                definition.StringData = serializedData;
            }
        }

        if (updatedWorkflowDefinitions.Any())
        {
            var definitionVersionsUpdates = updatedWorkflowDefinitions.Select(x => 
                new WorkflowDefinitionVersionUpdate(x.Id, x.DefinitionId, x.Options.UsableAsActivity.GetValueOrDefault())).ToList();
            await notificationSender.SendAsync(new WorkflowDefinitionVersionsUpdating(definitionVersionsUpdates), cancellationToken);
            await workflowDefinitionStore.SaveManyAsync(updatedWorkflowDefinitions, cancellationToken);
            await notificationSender.SendAsync(new WorkflowDefinitionVersionsUpdated(definitionVersionsUpdates), cancellationToken);
        }

        return updatedWorkflowDefinitions;
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