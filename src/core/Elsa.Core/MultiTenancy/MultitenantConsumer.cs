using System;
using Elsa.Abstractions.Multitenancy;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rebus.Pipeline;

namespace Elsa.MultiTenancy
{
    public abstract class MultitenantConsumer
    {
        protected IServiceProvider ServiceProvider { get; }

        protected MultitenantConsumer(IMessageContext messageContext, IServiceProvider serviceProvider) 
        { 
            ServiceProvider = serviceProvider;

            var tenant = JsonConvert.DeserializeObject<Tenant>(messageContext.Headers["tenant"]);

            var tenantProvider = ServiceProvider.GetRequiredService<ITenantProvider>();
            tenantProvider.SetCurrentTenantAsync(tenant!).GetAwaiter().GetResult();
        }
    }
}
