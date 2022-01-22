using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Events;
using Elsa.Models;
using Elsa.Providers.Workflows;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Services.Workflows
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        //public static Func<IWorkflowProvider, bool> SkipDynamicProviders => x => !x.GetType().GetCustomAttributes<SkipTriggerIndexingAttribute>().Any();
        private readonly IEnumerable<IWorkflowProvider> _workflowProviders;
        private readonly IMediator _mediator;

        public WorkflowRegistry(IEnumerable<IWorkflowProvider> workflowProviders, IMediator mediator)
        {
            _workflowProviders = workflowProviders;
            _mediator = mediator;
        }

        // public async Task<IEnumerable<IWorkflowBlueprint>> ListAsync(CancellationToken cancellationToken) => await ListInternalAsync(default, cancellationToken).ToListAsync(cancellationToken);
        
        // public async Task<IEnumerable<IWorkflowBlueprint>> ListAsync(Func<IWorkflowProvider, bool> includeProvider, CancellationToken cancellationToken = default) =>
        //     await ListInternalAsync(includeProvider, cancellationToken).ToListAsync(cancellationToken);
        //
        // public async Task<IEnumerable<IWorkflowBlueprint>> ListActiveAsync(CancellationToken cancellationToken) => await ListActiveInternalAsync(cancellationToken).ToListAsync(cancellationToken);
        //

        // public async Task<IEnumerable<IWorkflowBlueprint>> FindManyAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken) =>
        //     (await ListAsync(cancellationToken).Where(predicate).OrderByDescending(x => x.Version)).ToList();

        public async Task<IWorkflowBlueprint?> FindAsync(string definitionId, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var providers = _workflowProviders;

            foreach (var provider in providers)
            {
                var workflow = await provider.FindAsync(definitionId, versionOptions, tenantId, cancellationToken);

                if (workflow == null) 
                    continue;
                
                await _mediator.Publish(new WorkflowBlueprintLoaded(workflow), cancellationToken);
                return workflow;
            }

            return null;
        }

        // private async IAsyncEnumerable<IWorkflowBlueprint> ListActiveInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        // {
        //     var workflows = await ListInternalAsync(default, cancellationToken).ToListAsync(cancellationToken);
        //     var publishedWorkflows = workflows.Where(x => x.IsPublished);
        //
        //     foreach (var publishedWorkflow in publishedWorkflows)
        //         yield return publishedWorkflow;
        //
        //     // We also need to consider unpublished workflows for inclusion in case they still have associated active workflow instances.
        //     var unpublishedWorkflows = workflows.Where(x => !x.IsPublished).ToDictionary(x => x.VersionId);
        //     var unpublishedWorkflowIds = unpublishedWorkflows.Keys;
        //
        //     if (!unpublishedWorkflowIds.Any())
        //         yield break;
        //
        //     var activeWorkflowInstances = await _workflowInstanceStore.FindManyAsync(new UnfinishedWorkflowSpecification().WithWorkflowDefinitionVersionIds(unpublishedWorkflowIds), cancellationToken: cancellationToken).ToList();
        //     var activeUnpublishedWorkflowVersionIds = activeWorkflowInstances.Select(x => x.DefinitionVersionId).Distinct().ToList();
        //     var activeUnpublishedWorkflowVersions = unpublishedWorkflows.Where(x => activeUnpublishedWorkflowVersionIds.Contains(x.Key)).Select(x => x.Value);
        //
        //     foreach (var unpublishedWorkflow in activeUnpublishedWorkflowVersions)
        //         yield return unpublishedWorkflow;
        // }
        
        // private async IAsyncEnumerable<IWorkflowBlueprint> ListInternalAsync(Func<IWorkflowProvider, bool>? includeProvider, [EnumeratorCancellation] CancellationToken cancellationToken)
        // {
        //     var providers = _workflowProviders;
        //
        //     if (includeProvider != null)
        //         providers = providers.Where(includeProvider);
        //
        //     foreach (var provider in providers)
        //     await foreach (var workflow in provider.GetWorkflowsAsync(cancellationToken).WithCancellation(cancellationToken))
        //     {
        //         await _mediator.Publish(new WorkflowBlueprintLoaded(workflow), cancellationToken);
        //         yield return workflow;
        //     }
        // }
        //
        // private async Task<bool> WorkflowHasUnfinishedWorkflowsAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken)
        // {
        //     var count = await _workflowInstanceStore.CountAsync(new UnfinishedWorkflowSpecification().WithWorkflowDefinition(workflowBlueprint.Id), cancellationToken);
        //     return count > 0;
        // }
    }
}