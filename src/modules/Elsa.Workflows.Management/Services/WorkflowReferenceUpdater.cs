using System.Runtime.CompilerServices;
using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Services;

public class WorkflowReferenceUpdater(
    IWorkflowDefinitionPublisher publisher,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowDefinitionStore workflowDefinitionStore,
    IWorkflowReferenceQuery workflowReferenceQuery,
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

        var referencedWorkflowDefinitions = (await workflowDefinitionStore.FindManyAsync(new()
            {
            DefinitionIds = referencedIds,
            VersionOptions = VersionOptions.LatestOrPublished,
            IsReadonly = false
        }, cancellationToken))
        .ToDictionary(d => d.DefinitionId);

        var dependencyMap = filteredWorkflowReferences
            .SelectMany(r => r.ReferencingDefinitionIds.Select(id => (id, r.ReferencedDefinitionId)))
            .ToLookup(x => x.id, x => x.ReferencedDefinitionId);

        var updatedWorkflows = new HashSet<UpdatedWorkflowDefinition>();
        
        // Create a cache for drafts that we've already created during this operation
        var draftCache = new Dictionary<string, (WorkflowDefinition Draft, bool WasPublished)>();

        foreach (var id in referencingIds)
        {
            if (!referencingWorkflowGraphs.TryGetValue(id, out var graph) || !dependencyMap[id].Any())
                continue;

            foreach (var refId in dependencyMap[id])
            {
                var target = referencedWorkflowDefinitions.GetValueOrDefault(refId);
                if (target == null) continue;

                var updated = await UpdateWorkflowAsync(graph, target, draftCache, cancellationToken);
                if (updated == null) continue;

                updatedWorkflows.Add(updated);
                referencedWorkflowDefinitions[id] = updated.Definition;

                if (updated.Definition.Id != graph.Workflow.Id) 
                    referencingWorkflowGraphs[id] = await workflowDefinitionService.MaterializeWorkflowAsync(updated.Definition, cancellationToken);
            }
        }

        _isUpdating = true;
        foreach (var updatedWorkflow in updatedWorkflows)
        {
            if (updatedWorkflow.RequiresPublication)
                await publisher.PublishAsync(updatedWorkflow.Definition, cancellationToken);
            else
                await publisher.SaveDraftAsync(updatedWorkflow.Definition, cancellationToken);
        }
        _isUpdating = false;

        return new(updatedWorkflows.Select(u => u.Definition));
    }

    private async IAsyncEnumerable<WorkflowReferences> GetReferencingWorkflowDefinitionIdsAsync(
        string definitionId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var refs = (await workflowReferenceQuery.ExecuteAsync(definitionId, cancellationToken)).ToList();
        yield return new(definitionId, refs);
        foreach (var id in refs)
            await foreach (var child in GetReferencingWorkflowDefinitionIdsAsync(id, cancellationToken))
                yield return child;
    }

    private async Task<UpdatedWorkflowDefinition?> UpdateWorkflowAsync(
        WorkflowGraph graph,
        WorkflowDefinition target,
        Dictionary<string, (WorkflowDefinition Draft, bool WasPublished)> draftCache,
        CancellationToken cancellationToken)
    {
        var id = graph.Workflow.Identity.DefinitionId;
        var wasPublished = graph.Workflow.Publication.IsPublished;
        
        // Get the draft from cache or create a new one
        var draft = await GetOrCreateDraftAsync(id, wasPublished, draftCache, cancellationToken);
        if (draft == null) return null;
        
        var newGraph = await workflowDefinitionService.MaterializeWorkflowAsync(draft, cancellationToken);
        var outdated = FindActivities(newGraph.Root, target.DefinitionId)
            .Where(a => a.WorkflowDefinitionVersionId != target.Id)
            .ToList();
        
        if (!outdated.Any()) return null;

        foreach (var act in outdated)
        {
            act.WorkflowDefinitionVersionId = target.Id;
            act.LatestAvailablePublishedVersionId = target.Id;
            act.Version = target.Version;
        }

        if (newGraph.Root.Activity is Workflow wf)
            draft.StringData = serializer.Serialize(wf.Root);

        return new(draft, wasPublished);
    }
    
    private async Task<WorkflowDefinition?> GetOrCreateDraftAsync(
        string definitionId,
        bool wasPublished,
        Dictionary<string, (WorkflowDefinition Draft, bool WasPublished)> draftCache,
        CancellationToken cancellationToken)
    {
        // Check if we already have a draft for this workflow
        if (draftCache.TryGetValue(definitionId, out var cachedDraft))
        {
            return cachedDraft.Draft;
        }
        
        // Create or get a draft for this workflow
        var draft = await publisher.GetDraftAsync(definitionId, VersionOptions.Latest, cancellationToken);
        if (draft == null) return null;
        
        // Store the draft in the cache for potential future use
        draftCache[definitionId] = (draft, wasPublished);
        
        return draft;
    }

    private static IEnumerable<WorkflowDefinitionActivity> FindActivities(ActivityNode node, string definitionId)
    {
        foreach (var child in node.Children)
        {
            if (child.Activity is WorkflowDefinitionActivity activity && activity.WorkflowDefinitionId == definitionId)
                yield return activity;
            foreach (var grandChildActivity in FindActivities(child, definitionId))
                yield return grandChildActivity;
        }
    }
}

public record WorkflowReferences(string ReferencedDefinitionId, ICollection<string> ReferencingDefinitionIds);

public record UpdatedWorkflowDefinition(WorkflowDefinition Definition, bool RequiresPublication);
