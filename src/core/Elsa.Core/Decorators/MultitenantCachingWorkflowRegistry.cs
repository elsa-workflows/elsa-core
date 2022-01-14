using Elsa.Abstractions.MultiTenancy;
using Elsa.Caching;
using Elsa.Persistence;
using Elsa.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Decorators
{
    public class MultitenantCachingWorkflowRegistry : CachingWorkflowRegistry
    {
        private readonly ITenantProvider _tenantProvider;

        protected override string GenerateCacheKey() => $"{CacheKey}_{_tenantProvider.GetCurrentTenant().Prefix}";

        public MultitenantCachingWorkflowRegistry(
            IWorkflowRegistry workflowRegistry,
            IMemoryCache memoryCache,
            ICacheSignal cacheSignal,
            IWorkflowInstanceStore workflowInstanceStore,
            ITenantProvider tenantProvider)
            :base (workflowRegistry, memoryCache, cacheSignal, workflowInstanceStore)
        {
            _tenantProvider = tenantProvider;
        }
    }
}