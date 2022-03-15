using System;
using Elsa.Abstractions.Multitenancy;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rebus.Pipeline;

namespace Elsa.MultiTenancy
{
    public class MultitenantConsumer
    {
        protected readonly IServiceProvider _serviceProvider;

        public MultitenantConsumer(IMessageContext messageContext, IServiceProvider serviceProvider) 
        { 
            _serviceProvider = serviceProvider;

            var tenant = JsonConvert.DeserializeObject<Tenant>(messageContext.Headers["tenant"]);

            var tenantProvider = _serviceProvider.GetRequiredService<ITenantProvider>();
            tenantProvider.SetCurrentTenantAsync(tenant!).GetAwaiter().GetResult();
        }
    }
}
