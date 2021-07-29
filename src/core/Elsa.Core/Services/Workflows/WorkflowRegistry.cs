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
using Elsa.Providers.Workflow;
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

        public async Task<IEnumerable<IWorkflowBlueprint>> ListAsync(CancellationToken cancellationToken, bool includeDisabled = false) => await ListInternalAsync(cancellationToken, includeDisabled).ToListAsync(cancellationToken);
        public async Task<IEnumerable<IWorkflowBlueprint>> ListActiveAsync(CancellationToken cancellationToken, bool includeDisabled = false) => await ListActiveInternalAsync(cancellationToken, includeDisabled).ToListAsync(cancellationToken);

        public async Task<IWorkflowBlueprint?> GetAsync(string id, string? tenantId, VersionOptions version, CancellationToken cancellationToken, bool includeDisabled = false) =>
            await FindAsync(x => x.Id == id && x.TenantId == tenantId && x.WithVersion(version), cancellationToken, includeDisabled);

        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken, bool includeDisabled = false) =>
            (await ListAsync(cancellationToken, includeDisabled).Where(predicate).OrderByDescending(x => x.Version)).ToList();

        public async Task<IWorkflowBlueprint?> FindAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken, bool includeDisabled = false) =>
            (await ListAsync(cancellationToken, includeDisabled).Where(predicate).OrderByDescending(x => x.Version)).FirstOrDefault();
        
        private async IAsyncEnumerable<IWorkflowBlueprint> ListActiveInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken, bool includeDisabled)
        {
            var workflows = await ListAsync(cancellationToken, includeDisabled);

            foreach (var workflow in workflows)
            {
                // If a workflow is not published, only consider it for processing if it has at least one non-ended workflow instance.
                if (!workflow.IsPublished && !await WorkflowHasUnfinishedWorkflowsAsync(workflow, cancellationToken))
                    continue;

                yield return workflow;  
            }
        }

        private async IAsyncEnumerable<IWorkflowBlueprint> ListInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken, bool includeDisabled)
        {   
            var providers = _workflowProviders;

            foreach (var provider in providers)
                await foreach (var workflow in provider.GetWorkflowsAsync(cancellationToken).WithCancellation(cancellationToken))
                {
                    var workflowSettingsContext = new WorkflowSettingsContext(workflow.Id);
                    await _mediator.Publish(new WorkflowSettingsLoaded(workflowSettingsContext), cancellationToken);
                    workflow.IsDisabled = workflowSettingsContext.Value;

                    if (!includeDisabled && workflowSettingsContext.Value)
                        continue;

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