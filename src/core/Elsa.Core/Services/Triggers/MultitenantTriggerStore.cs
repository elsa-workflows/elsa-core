using Elsa.Abstractions.MultiTenancy;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Services.Triggers
{
    public class MultitenantTriggerStore : TriggerStore
    {
        protected override string Key => $"{base.Key}_{_tenantProvider.GetCurrentTenant().Prefix}";
        private readonly ITenantProvider _tenantProvider;

        public MultitenantTriggerStore(IMemoryCache memoryCache, ITenantProvider tenantProvider) : base(memoryCache)
        {
            _tenantProvider = tenantProvider;
        }
    }
}