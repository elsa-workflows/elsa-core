using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Open.Linq.AsyncExtensions;

namespace Elsa.Decorators
{
    public class CachingWorkflowRegistry : IWorkflowRegistry, INotificationHandler<WorkflowDefinitionSaved>, INotificationHandler<WorkflowDefinitionDeleted>
    {
        public const string CacheKey = "WorkflowRegistry";
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IMemoryCache _memoryCache;
        private readonly ICacheSignal _cacheSignal;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IMediator _mediator;

        public CachingWorkflowRegistry(
            IWorkflowRegistry workflowRegistry, 
            IMemoryCache memoryCache, 
            ICacheSignal cacheSignal, 
            IWorkflowInstanceStore workflowInstanceStore, 
            IMediator mediator)
        {
            _workflowRegistry = workflowRegistry;
            _memoryCache = memoryCache;
            _cacheSignal = cacheSignal;
            _workflowInstanceStore = workflowInstanceStore;
            _mediator = mediator;
        }

        public async Task<IEnumerable<IWorkflowBlueprint>> ListAsync(bool includeDisabled, CancellationToken cancellationToken) => await ListInternalAsync(includeDisabled, cancellationToken);
        public async Task<IEnumerable<IWorkflowBlueprint>> ListActiveAsync(bool includeDisabled, CancellationToken cancellationToken = default) => await ListActiveInternalAsync(includeDisabled, cancellationToken).ToListAsync(cancellationToken);

        public async Task<IWorkflowBlueprint?> GetAsync(string id, string? tenantId, VersionOptions version, bool includeDisabled, CancellationToken cancellationToken) =>
            await FindAsync(x => x.Id == id && x.TenantId == tenantId && x.WithVersion(version), includeDisabled, cancellationToken);

        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyAsync(Func<IWorkflowBlueprint, bool> predicate, bool includeDisabled, CancellationToken cancellationToken)
        {
            var workflows = await ListInternalAsync(includeDisabled, cancellationToken);
            return workflows.Where(predicate);
        }

        public async Task<IWorkflowBlueprint?> FindAsync(Func<IWorkflowBlueprint, bool> predicate, bool includeDisabled, CancellationToken cancellationToken)
        {
            var workflows = await ListInternalAsync(includeDisabled, cancellationToken);
            return workflows.FirstOrDefault(predicate);
        }

        private async Task<ICollection<IWorkflowBlueprint>> ListInternalAsync(bool includeDisabled, CancellationToken cancellationToken)
        {
            return await _memoryCache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.Monitor(_cacheSignal.GetToken(CacheKey));
                return await _workflowRegistry.ListAsync(includeDisabled, cancellationToken).ToList();
            });
        }
        
        private async IAsyncEnumerable<IWorkflowBlueprint> ListActiveInternalAsync(bool includeDisabled, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var workflows = await ListInternalAsync(includeDisabled, cancellationToken);
            
            foreach (var workflow in workflows)
            {
                // If a workflow is not published, only consider it for processing if it has at least one non-ended workflow instance.
                if (!workflow.IsPublished && !await WorkflowHasUnfinishedWorkflowsAsync(workflow, cancellationToken))
                    continue;

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

        Task INotificationHandler<WorkflowDefinitionSaved>.Handle(WorkflowDefinitionSaved notification, CancellationToken cancellationToken)
        {
            _cacheSignal.TriggerTokenAsync(CacheKey);
            return Task.CompletedTask;
        }

        Task INotificationHandler<WorkflowDefinitionDeleted>.Handle(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken)
        {
            _cacheSignal.TriggerTokenAsync(CacheKey);
            return Task.CompletedTask;
        }
    }
}