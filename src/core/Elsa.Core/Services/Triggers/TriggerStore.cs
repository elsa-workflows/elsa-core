using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.MultiTenancy;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Services.Triggers
{
    public class TriggerStore : ITriggerStore
    {
        private string Key => $"WorkflowTriggerStore_{_tenantProvider.GetCurrentTenant().Prefix}";
        private readonly IMemoryCache _memoryCache;
        private readonly ITenantProvider _tenantProvider;

        public TriggerStore(IMemoryCache memoryCache, ITenantProvider tenantProvider)
        {
            _memoryCache = memoryCache;
            _tenantProvider = tenantProvider;
        }
        
        public ValueTask StoreAsync(IEnumerable<WorkflowTrigger> triggers, CancellationToken cancellationToken = default)
        {
            _memoryCache.Set(Key, triggers.ToList());
            return new ValueTask();
        }

        public ValueTask<IEnumerable<WorkflowTrigger>> GetAsync(CancellationToken cancellationToken = default)
        {
            var result = _memoryCache.Get<IEnumerable<WorkflowTrigger>>(Key) ?? Enumerable.Empty<WorkflowTrigger>();
            return new ValueTask<IEnumerable<WorkflowTrigger>>(result);
        }
    }
}