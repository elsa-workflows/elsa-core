using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Providers.Workflows;
using Elsa.Services.Models;
using MediatR;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services.Workflows
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private readonly IEnumerable<IWorkflowProvider> _workflowProviders;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IMediator _mediator;

        public WorkflowRegistry(IEnumerable<IWorkflowProvider> workflowProviders, IWorkflowInstanceStore workflowInstanceStore, IMediator mediator)
        {
            _workflowProviders = workflowProviders;
            _workflowInstanceStore = workflowInstanceStore;
            _mediator = mediator;
        }

        public async Task<IEnumerable<IWorkflowBlueprint>> ListAsync(CancellationToken cancellationToken) => await ListInternalAsync(cancellationToken).ToListAsync(cancellationToken);
        public async Task<IEnumerable<IWorkflowBlueprint>> ListActiveAsync(CancellationToken cancellationToken) => await ListActiveInternalAsync(cancellationToken).ToListAsync(cancellationToken);

        public async Task<IWorkflowBlueprint?> GetAsync(string id, string? tenantId, VersionOptions version, CancellationToken cancellationToken, bool includeDisabled = false)
        {
            return !includeDisabled
                ? await FindAsync(x => x.Id == id && x.TenantId == tenantId && x.WithVersion(version) && !x.IsDisabled, cancellationToken)
                : await FindAsync(x => x.Id == id && x.TenantId == tenantId && x.WithVersion(version), cancellationToken);
        }

        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken) =>
            (await ListAsync(cancellationToken).Where(predicate).OrderByDescending(x => x.Version)).ToList();

        public async Task<IWorkflowBlueprint?> FindAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken) =>
            (await ListAsync(cancellationToken).Where(predicate).OrderByDescending(x => x.Version)).FirstOrDefault();

        private async IAsyncEnumerable<IWorkflowBlueprint> ListActiveInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var workflows = await ListInternalAsync(cancellationToken).ToListAsync(cancellationToken);
            var publishedWorkflows = workflows.Where(x => x.IsPublished);

            foreach (var publishedWorkflow in publishedWorkflows)
                yield return publishedWorkflow;

            // We also need to consider unpublished workflows for inclusion in case they still have associated active workflow instances.
            var unpublishedWorkflows = workflows.Where(x => !x.IsPublished).ToDictionary(x => x.VersionId);
            var unpublishedWorkflowIds = unpublishedWorkflows.Keys;

            if (!unpublishedWorkflowIds.Any())
                yield break;

            var activeWorkflowInstances = await _workflowInstanceStore.FindManyAsync(new UnfinishedWorkflowSpecification().WithWorkflowDefinitionVersionIds(unpublishedWorkflowIds), cancellationToken: cancellationToken).ToList();
            var activeUnpublishedWorkflowVersionIds = activeWorkflowInstances.Select(x => x.DefinitionVersionId).Distinct().ToList();
            var activeUnpublishedWorkflowVersions = unpublishedWorkflows.Where(x => activeUnpublishedWorkflowVersionIds.Contains(x.Key)).Select(x => x.Value);

            foreach (var unpublishedWorkflow in activeUnpublishedWorkflowVersions)
                yield return unpublishedWorkflow;
        }

        private async IAsyncEnumerable<IWorkflowBlueprint> ListInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var providers = _workflowProviders;

            foreach (var provider in providers)
            await foreach (var workflow in provider.GetWorkflowsAsync(cancellationToken).WithCancellation(cancellationToken))
            {
                await _mediator.Publish(new WorkflowBlueprintLoaded(workflow), cancellationToken);
                yield return workflow;
            }
        }

        private async Task<bool> WorkflowHasUnfinishedWorkflowsAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken)
        {
            var count = await _workflowInstanceStore.CountAsync(new UnfinishedWorkflowSpecification().WithWorkflowDefinition(workflowBlueprint.Id), cancellationToken);
            return count > 0;
        }
    }
}