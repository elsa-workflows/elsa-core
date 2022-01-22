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
using Elsa.Providers.Workflows;
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

        public CachingWorkflowRegistry(
            IWorkflowRegistry workflowRegistry,
            IMemoryCache memoryCache,
            ICacheSignal cacheSignal,
            IWorkflowInstanceStore workflowInstanceStore)
        {
            _workflowRegistry = workflowRegistry;
            _memoryCache = memoryCache;
            _cacheSignal = cacheSignal;
            _workflowInstanceStore = workflowInstanceStore;
        }
        
        public async Task<IWorkflowBlueprint?> FindAsync(string definitionId, VersionOptions version, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            return await _memoryCache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.Monitor(_cacheSignal.GetToken(CacheKey));
                return await _workflowRegistry.FindAsync(definitionId, version, tenantId, cancellationToken);
            });
        }

        // public async Task<IEnumerable<IWorkflowBlueprint>> ListAsync(CancellationToken cancellationToken) => await ListInternalAsync(cancellationToken);
        // public Task<IEnumerable<IWorkflowBlueprint>> ListAsync(Func<IWorkflowProvider, bool> includeProvider, CancellationToken cancellationToken = default)
        // {
        //     throw new NotImplementedException();
        // }
        //
        // public async Task<IEnumerable<IWorkflowBlueprint>> ListActiveAsync(CancellationToken cancellationToken = default) => await ListActiveInternalAsync(cancellationToken).ToListAsync(cancellationToken);
        //
        // public async Task<IWorkflowBlueprint?> GetAsync(string id, string? tenantId, VersionOptions version, CancellationToken cancellationToken, bool includeDisabled = false) =>
        //     await FindAsync(x => x.Id == id && x.TenantId == tenantId && x.WithVersion(version), cancellationToken);
        //
        // public async Task<IEnumerable<IWorkflowBlueprint>> FindManyAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken)
        // {
        //     var workflows = await ListInternalAsync(cancellationToken);
        //     return workflows.Where(predicate);
        // }

        // public async Task<IWorkflowBlueprint?> FindAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken)
        // {
        //     var workflows = await ListInternalAsync(cancellationToken);
        //     return workflows.FirstOrDefault(predicate);
        // }
        //
        // private async Task<ICollection<IWorkflowBlueprint>> ListInternalAsync(CancellationToken cancellationToken)
        // {
        //     return await _memoryCache.GetOrCreateAsync(CacheKey, async entry =>
        //     {
        //         entry.Monitor(_cacheSignal.GetToken(CacheKey));
        //         return await _workflowRegistry.ListAsync(cancellationToken).ToList();
        //     });
        // }
        //
        // private async IAsyncEnumerable<IWorkflowBlueprint> ListActiveInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        // {
        //     var workflows = await ListInternalAsync(cancellationToken);
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