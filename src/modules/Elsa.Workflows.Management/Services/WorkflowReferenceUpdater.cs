using System.Runtime.CompilerServices;
using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowReferenceUpdater(
    IWorkflowDefinitionPublisher publisher,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowDefinitionStore workflowDefinitionStore,
    IWorkflowReferenceQuery workflowReferenceQuery,
    IApiSerializer serializer) : IWorkflowReferenceUpdater
{
    private bool _isUpdating;

    /// <inheritdoc />
    public async Task<UpdateWorkflowReferencesResult> UpdateWorkflowReferencesAsync(WorkflowDefinition referencedDefinition, CancellationToken cancellationToken = default)
    {
        // Prevent concurrent updates.
        if (_isUpdating)
            return new([]);

        // Skip if the published workflow definition is not usable as an activity or does not auto-update consuming workflows.
        if (referencedDefinition.Options is not { UsableAsActivity: true, AutoUpdateConsumingWorkflows: true })
            return new([]);

        // Find all workflow definitions that reference the updated workflow definition, directly or indirectly.
        var workflowReferences = (await GetReferencingWorkflowDefinitionIdsAsync(referencedDefinition.DefinitionId, cancellationToken).ToListAsync(cancellationToken: cancellationToken)).Where(x => x.ReferencingDefinitionIds.Count > 0).DistinctBy(x => x.ReferencedDefinitionId).ToList();
        var referencingWorkflowDefinitionIds = workflowReferences.SelectMany(x => x.ReferencingDefinitionIds).Distinct().ToList();
        var referencedWorkflowDefinitionIds = workflowReferences.Select(x => x.ReferencedDefinitionId).Distinct().ToList();
        var referencingWorkflowDefinitionsFilter = new WorkflowDefinitionFilter
        {
            DefinitionIds = referencingWorkflowDefinitionIds,
            VersionOptions = VersionOptions.Latest,
            IsReadonly = false
        };
        var referencedWorkflowDefinitionsFilter = new WorkflowDefinitionFilter
        {
            DefinitionIds = referencedWorkflowDefinitionIds,
            VersionOptions = VersionOptions.LatestOrPublished,
            IsReadonly = false
        };
        var referencingWorkflowGraphs = (await workflowDefinitionService.FindWorkflowGraphsAsync(referencingWorkflowDefinitionsFilter, cancellationToken)).ToDictionary(x => x.Workflow.Identity.DefinitionId);
        var referencedWorkflowDefinitions = (await workflowDefinitionStore.FindManyAsync(referencedWorkflowDefinitionsFilter, cancellationToken)).ToDictionary(x => x.DefinitionId);
        var updatedWorkflows = new HashSet<UpdatedWorkflowDefinition>();
        
        // Track updated workflow definitions to update references to them
        var updatedDefinitions = new Dictionary<string, WorkflowDefinition> {
            // Add the initial updated definition
            [referencedDefinition.DefinitionId] = referencedDefinition
        };

        // Build a dictionary of workflow references where key is the referencing workflow and value is a list of workflows it references
        var workflowDependencyMap = new Dictionary<string, List<string>>();
        
        foreach (var workflowReference in workflowReferences)
        {
            foreach (var referencingDefinitionId in workflowReference.ReferencingDefinitionIds)
            {
                if (!workflowDependencyMap.TryGetValue(referencingDefinitionId, out var dependencies))
                {
                    dependencies = new();
                    workflowDependencyMap[referencingDefinitionId] = dependencies;
                }
                
                dependencies.Add(workflowReference.ReferencedDefinitionId);
            }
        }
        
        // Process all workflows without topological sorting
        foreach (var referencingDefinitionId in referencingWorkflowDefinitionIds)
        {
            if (!referencingWorkflowGraphs.TryGetValue(referencingDefinitionId, out var referencingWorkflowGraph))
                continue;
            
            // Find all referenced workflow definitions for this workflow
            if (!workflowDependencyMap.TryGetValue(referencingDefinitionId, out var referencedDefinitionIds))
                continue;
            
            foreach (var referencedDefinitionId in referencedDefinitionIds)
            {
                // Get the most up-to-date version of the referenced workflow definition
                WorkflowDefinition referencedWorkflowDefinition;
                
                // If this workflow was already updated in this process, use the updated version
                if (updatedDefinitions.TryGetValue(referencedDefinitionId, out var updatedDefinition))
                {
                    referencedWorkflowDefinition = updatedDefinition;
                }
                else if (referencedWorkflowDefinitions.TryGetValue(referencedDefinitionId, out var originalDefinition))
                {
                    referencedWorkflowDefinition = originalDefinition;
                }
                else
                {
                    continue; // Skip if we can't find this referenced workflow
                }

                var updatedWorkflow = await UpdateReferencingWorkflowAsync(referencingWorkflowGraph, referencedWorkflowDefinition, cancellationToken);

                if (updatedWorkflow != null)
                {
                    updatedWorkflows.Add(updatedWorkflow);
                    
                    // Store the updated definition so that any workflows referencing it will use this updated version
                    updatedDefinitions[referencingDefinitionId] = updatedWorkflow.Definition;
                    
                    // Also update the workflow graph in our dictionary so that future updates to this workflow
                    // will start from the latest version
                    if (updatedWorkflow.Definition.Id != referencingWorkflowGraph.Workflow.Id)
                    {
                        // Materialize the updated workflow
                        var updatedWorkflowGraph = await workflowDefinitionService.MaterializeWorkflowAsync(updatedWorkflow.Definition, cancellationToken);
                        referencingWorkflowGraphs[referencingDefinitionId] = updatedWorkflowGraph;
                    }
                }
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

        return new(updatedWorkflows.Select(x => x.Definition));
    }

    private async IAsyncEnumerable<WorkflowReferences> GetReferencingWorkflowDefinitionIdsAsync(string workflowDefinitionId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var referencedDefinitionIds = (await workflowReferenceQuery.ExecuteAsync(workflowDefinitionId, cancellationToken)).ToList();

        yield return new(workflowDefinitionId, referencedDefinitionIds);

        foreach (var definitionId in referencedDefinitionIds)
        {
            // Recursively get referencing workflow definitions.
            await foreach (var workflowReferences in GetReferencingWorkflowDefinitionIdsAsync(definitionId, cancellationToken))
                yield return workflowReferences;
        }
    }

    private async Task<UpdatedWorkflowDefinition?> UpdateReferencingWorkflowAsync(WorkflowGraph referencingWorkflowGraph, WorkflowDefinition referencedWorkflowDefinition, CancellationToken cancellationToken)
    {
        // Create a new version of the published workflow definition or get the existing draft.
        var referencingWorkflowDefinitionId = referencingWorkflowGraph.Workflow.Identity.DefinitionId;
        var originalVersionIsPublished = referencingWorkflowGraph.Workflow.Publication.IsPublished;
        var newVersion = await publisher.GetDraftAsync(referencingWorkflowDefinitionId, VersionOptions.LatestOrPublished, cancellationToken);

        // This is null in case the definition no longer exists in the store.
        if (newVersion == null)
            return null;

        // Materialize the draft to find all workflow definition activities that use the updated workflow definition.
        var newWorkflowGraph = await workflowDefinitionService.MaterializeWorkflowAsync(newVersion, cancellationToken);
        var outdatedWorkflowDefinitionActivities = FindOutdatedWorkflowDefinitionActivities(newWorkflowGraph, referencedWorkflowDefinition).ToList();

        // Skip if the new version of the published workflow definition is not used in the workflow or if the activity is already up to date.
        if (outdatedWorkflowDefinitionActivities.Count == 0)
            return null;

        // Update the consuming workflow graph to use the new version of the published workflow definition.
        foreach (var workflowDefinitionActivity in outdatedWorkflowDefinitionActivities)
        {
            workflowDefinitionActivity.WorkflowDefinitionVersionId = referencedWorkflowDefinition.Id;
            workflowDefinitionActivity.LatestAvailablePublishedVersionId = referencedWorkflowDefinition.Id;
            workflowDefinitionActivity.Version = referencedWorkflowDefinition.Version;
        }

        // Update the new version of the published workflow definition.
        if (newWorkflowGraph.Root.Activity is Workflow newWorkflow)
            newVersion.StringData = serializer.Serialize(newWorkflow.Root);

        return new(newVersion, originalVersionIsPublished);
    }

    private IEnumerable<WorkflowDefinitionActivity> FindOutdatedWorkflowDefinitionActivities(WorkflowGraph workflowGraph, WorkflowDefinition updatedDefinition)
    {
        return FindWorkflowActivityDefinitionActivityNodes(workflowGraph.Root)
            .Where(x => x.WorkflowDefinitionId == updatedDefinition.DefinitionId && x.WorkflowDefinitionVersionId != updatedDefinition.Id);
    }

    private IEnumerable<WorkflowDefinitionActivity> FindWorkflowActivityDefinitionActivityNodes(ActivityNode parent)
    {
        foreach (var child in parent.Children)
        {
            if (child.Activity is WorkflowDefinitionActivity workflowDefinitionActivity)
                yield return workflowDefinitionActivity;
            else
            {
                foreach (var grandChild in FindWorkflowActivityDefinitionActivityNodes(child))
                    yield return grandChild;
            }
        }
    }
}

public record WorkflowReferences(string ReferencedDefinitionId, ICollection<string> ReferencingDefinitionIds);

public record UpdatedWorkflowDefinition(WorkflowDefinition Definition, bool RequiresPublication);