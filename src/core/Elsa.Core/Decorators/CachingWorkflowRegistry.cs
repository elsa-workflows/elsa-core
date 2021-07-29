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

        public async Task<IEnumerable<IWorkflowBlueprint>> ListAsync(CancellationToken cancellationToken, bool includeDisabled = false) => await ListInternalAsync(cancellationToken, includeDisabled);
        public async Task<IEnumerable<IWorkflowBlueprint>> ListActiveAsync(CancellationToken cancellationToken = default, bool includeDisabled = false) => await ListActiveInternalAsync(cancellationToken, includeDisabled).ToListAsync(cancellationToken);

        public async Task<IWorkflowBlueprint?> GetAsync(string id, string? tenantId, VersionOptions version, CancellationToken cancellationToken, bool includeDisabled = false) =>
            await FindAsync(x => x.Id == id && x.TenantId == tenantId && x.WithVersion(version), cancellationToken, includeDisabled);

        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken, bool includeDisabled = false)
        {
            var workflows = await ListInternalAsync(cancellationToken, includeDisabled);
            return workflows.Where(predicate);
        }

        public async Task<IWorkflowBlueprint?> FindAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken, bool includeDisabled = false)
        {
            var workflows = await ListInternalAsync(cancellationToken, includeDisabled);
            return workflows.FirstOrDefault(predicate);
        }

        private async Task<ICollection<IWorkflowBlueprint>> ListInternalAsync(CancellationToken cancellationToken, bool includeDisabled)
        {
            return await _memoryCache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.Monitor(_cacheSignal.GetToken(CacheKey));
                return await _workflowRegistry.ListAsync(cancellationToken, includeDisabled).ToList();
            });
        }
        
        private async IAsyncEnumerable<IWorkflowBlueprint> ListActiveInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken, bool includeDisabled)
        {
            var workflows = await ListInternalAsync(cancellationToken, includeDisabled);
            
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