using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Caching.Memory;
using Open.Linq.AsyncExtensions;

namespace Elsa.Decorators
{
    public class CachingWorkflowRegistry : IWorkflowRegistry
    {
        private const string CacheKey = "WorkflowRegistry";
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IMemoryCache _memoryCache;
        private readonly ISignal _signal;

        public CachingWorkflowRegistry(IWorkflowRegistry workflowRegistry, IMemoryCache memoryCache, ISignal signal)
        {
            _workflowRegistry = workflowRegistry;
            _memoryCache = memoryCache;
            _signal = signal;
        }

        public async Task<IEnumerable<IWorkflowBlueprint>> ListAsync(CancellationToken cancellationToken) => await GetWorkflowBlueprints(cancellationToken);

        public async Task<IWorkflowBlueprint?> GetAsync(string id, string? tenantId, VersionOptions version, CancellationToken cancellationToken) =>
            await FindAsync(x => x.Id == id && x.TenantId == tenantId && x.WithVersion(version), cancellationToken);

        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken)
        {
            var workflows = await GetWorkflowBlueprints(cancellationToken);
            return workflows.Where(predicate);
        }

        public async Task<IWorkflowBlueprint?> FindAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken)
        {
            var workflows = await GetWorkflowBlueprints(cancellationToken);
            return workflows.FirstOrDefault(predicate);
        }

        private async Task<ICollection<IWorkflowBlueprint>> GetWorkflowBlueprints(CancellationToken cancellationToken)
        {
            return await _memoryCache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.Monitor(_signal.GetToken(CacheKey));
                return await _workflowRegistry.ListAsync(cancellationToken).ToList();
            });
        }
    }
}