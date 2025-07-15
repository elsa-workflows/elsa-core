using System.Runtime.CompilerServices;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Services;

internal record WorkflowReferences(string ReferencedDefinitionId, ICollection<string> ReferencingDefinitionIds);

internal record UpdatedWorkflowDefinition(WorkflowDefinition Definition, WorkflowGraph NewGraph);

public class WorkflowReferenceUpdater(
    IWorkflowDefinitionPublisher publisher,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowDefinitionStore workflowDefinitionStore,
    IWorkflowReferenceQuery workflowReferenceQuery,
    WorkflowDefinitionActivityDescriptorFactory workflowDefinitionActivityDescriptorFactory,
    IActivityRegistry activityRegistry,
    IApiSerializer serializer)
    : IWorkflowReferenceUpdater
{
    private bool _isUpdating;

    public async Task<UpdateWorkflowReferencesResult> UpdateWorkflowReferencesAsync(
        WorkflowDefinition referencedDefinition,
        CancellationToken cancellationToken = default)
    {
        if (_isUpdating ||
            referencedDefinition.Options is not { UsableAsActivity: true, AutoUpdateConsumingWorkflows: true })
            return new([]);

        var allWorkflowReferences = await GetReferencingWorkflowDefinitionIdsAsync(referencedDefinition.DefinitionId, cancellationToken).ToListAsync(cancellationToken);
        var filteredWorkflowReferences = allWorkflowReferences
            .Where(r => r.ReferencingDefinitionIds.Any())
            .DistinctBy(r => r.ReferencedDefinitionId)
            .ToList();

        var referencingIds = filteredWorkflowReferences.SelectMany(r => r.ReferencingDefinitionIds).Distinct().ToList();
        var referencedIds = filteredWorkflowReferences.Select(r => r.ReferencedDefinitionId).Distinct().ToList();

        var referencingWorkflowGraphs = (await workflowDefinitionService.FindWorkflowGraphsAsync(new()
            {
                DefinitionIds = referencingIds,
                VersionOptions = VersionOptions.Latest,
                IsReadonly = false
            }, cancellationToken))
            .ToDictionary(g => g.Workflow.Identity.DefinitionId);

        var referencedWorkflowDefinitionList = (await workflowDefinitionStore.FindManyAsync(new()
        {
            DefinitionIds = referencedIds,
            VersionOptions = VersionOptions.Published,
            IsReadonly = false
        }, cancellationToken)).ToList();

        var referencedWorkflowDefinitionsPublished = referencedWorkflowDefinitionList
            .GroupBy(x => x.DefinitionId)
            .Select(group =>
            {
                var publishedVersion = group.FirstOrDefault(x => x.IsPublished);
                return publishedVersion ?? group.First();
            })
            .ToDictionary(d => d.DefinitionId);

        var initialPublicationState = new Dictionary<string, bool>();

        foreach (var workflowGraph in referencingWorkflowGraphs)
            initialPublicationState[workflowGraph.Key] = workflowGraph.Value.Workflow.Publication.IsPublished;

        // Add the initially referenced definition
        referencedWorkflowDefinitionsPublished[referencedDefinition.DefinitionId] = referencedDefinition;

        // Build dependency map for topological sorting
        var dependencyMap = filteredWorkflowReferences
            .SelectMany(r => r.ReferencingDefinitionIds.Select(id => (id, r.ReferencedDefinitionId)))
            .ToLookup(x => x.id, x => x.ReferencedDefinitionId);

        // Perform topological sort to ensure dependent workflows are processed in the right order
        var sortedWorkflowIds = referencingIds
            .TSort(id => dependencyMap[id], true)
            // Only process workflows that exist in our referencing workflows dictionary
            .Where(id => referencingWorkflowGraphs.ContainsKey(id))
            .ToList();

        var updatedWorkflows = new Dictionary<string, UpdatedWorkflowDefinition>();

        // Create a cache for drafts that we've already created during this operation
        var draftCache = new Dictionary<string, WorkflowDefinition>();

        foreach (var id in sortedWorkflowIds)
        {
            if (!referencingWorkflowGraphs.TryGetValue(id, out var graph) || !dependencyMap[id].Any())
                continue;

            foreach (var refId in dependencyMap[id])
            {
                var target = referencedWorkflowDefinitionsPublished.GetValueOrDefault(refId);
                if (target == null) continue;

                var updated = await UpdateWorkflowAsync(graph, target, draftCache, initialPublicationState, cancellationToken);
                if (updated == null) continue;

                graph = updated.NewGraph;
                updatedWorkflows[updated.Definition.DefinitionId] = updated;
                referencedWorkflowDefinitionsPublished[id] = updated.Definition;
                draftCache[id] = updated.Definition;
                referencingWorkflowGraphs[id] = await workflowDefinitionService.MaterializeWorkflowAsync(updated.Definition, cancellationToken);
            }
        }

        _isUpdating = true;
        foreach (var updatedWorkflow in updatedWorkflows.Values)
        {
            var requiresPublication = initialPublicationState.GetValueOrDefault(updatedWorkflow.Definition.DefinitionId);
            if (requiresPublication)
                await publisher.PublishAsync(updatedWorkflow.Definition, cancellationToken);
            else
                await publisher.SaveDraftAsync(updatedWorkflow.Definition, cancellationToken);
        }

        _isUpdating = false;

        return new(updatedWorkflows.Select(u => u.Value.Definition));
    }

    private async IAsyncEnumerable<WorkflowReferences> GetReferencingWorkflowDefinitionIdsAsync(
        string definitionId,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        HashSet<string>? visitedIds = null)
    {
        visitedIds ??= new();

        // If we've already processed this definition ID, skip it to prevent infinite recursion.
        if (!visitedIds.Add(definitionId))
            yield break;

        var refs = (await workflowReferenceQuery.ExecuteAsync(definitionId, cancellationToken)).ToList();
        yield return new(definitionId, refs);

        foreach (var id in refs)
        {
            await foreach (var child in GetReferencingWorkflowDefinitionIdsAsync(id, cancellationToken, visitedIds))
                yield return child;
        }
    }

    private async Task<UpdatedWorkflowDefinition?> UpdateWorkflowAsync(
        WorkflowGraph graph,
        WorkflowDefinition target,
        Dictionary<string, WorkflowDefinition> draftCache,
        Dictionary<string, bool> initialPublicationState,
        CancellationToken cancellationToken)
    {
        var willTargetBePublished = initialPublicationState.GetValueOrDefault(target.DefinitionId, target.IsPublished);
        if (!willTargetBePublished)
            return null;

        var id = graph.Workflow.Identity.DefinitionId;
        var draft = await GetOrCreateDraftAsync(id, draftCache, cancellationToken);
        if (draft == null) return null;

        var newGraph = await workflowDefinitionService.MaterializeWorkflowAsync(draft, cancellationToken);
        var outdated = FindActivities(newGraph.Root, target.DefinitionId)
            .Where(a => a.WorkflowDefinitionVersionId != target.Id)
            .ToList();

        if (!outdated.Any()) return null;

        foreach (var act in outdated)
        {
            act.WorkflowDefinitionVersionId = target.Id;
            act.Version = target.Version;
            act.LatestAvailablePublishedVersionId = target.Id;
            act.LatestAvailablePublishedVersion = target.Version;
        }

        if (newGraph.Root.Activity is Workflow wf)
            draft.StringData = serializer.Serialize(wf.Root);

        return new(draft, newGraph);
    }

    private async Task<WorkflowDefinition?> GetOrCreateDraftAsync(
        string definitionId,
        Dictionary<string, WorkflowDefinition> draftCache,
        CancellationToken cancellationToken)
    {
        // Check if we already have a draft for this workflow
        if (draftCache.TryGetValue(definitionId, out var cachedDraft))
            return cachedDraft;

        // Create or get a draft for this workflow
        var draft = await publisher.GetDraftAsync(definitionId, VersionOptions.Latest, cancellationToken);
        if (draft == null) return null;

        // Store the draft in the cache for potential future use
        draftCache[definitionId] = draft;

        // Get the current published version of the workflow definition.
        var publishedVersion = await workflowDefinitionStore.FindAsync(
            WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published).ToFilter(),
            cancellationToken);

        // Update the activity registry to be able to materialize the workflow.
        var activityDescriptor = workflowDefinitionActivityDescriptorFactory.CreateDescriptor(draft, publishedVersion);
        activityRegistry.Add(typeof(WorkflowDefinitionActivityProvider), activityDescriptor);

        return draft;
    }

    private static IEnumerable<WorkflowDefinitionActivity> FindActivities(ActivityNode node, string definitionId)
    {
        // Do not drill into activities that are WorkflowDefinitionActivity
        if (node.Activity is WorkflowDefinitionActivity)
            yield break;

        foreach (var child in node.Children)
        {
            if (child.Activity is WorkflowDefinitionActivity activity && activity.WorkflowDefinitionId == definitionId)
                yield return activity;
            foreach (var grandChildActivity in FindActivities(child, definitionId))
                yield return grandChildActivity;
        }
    }
}