using Elsa.Common.Multitenancy;
using Elsa.Tenants;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MassTransit.Middleware;

public class TenantConsumeMiddleware<T>(ITenantAccessor tenantAccessor, IServiceScopeFactory scopeFactory) : IFilter<ConsumeContext<T>> where T : class
{
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("tenantConsume");
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        if (context.Headers.TryGetHeader("TenantId", out var tenantId) && tenantId is string tenantIdString)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var tenantsProvider = scope.ServiceProvider.GetRequiredService<ITenantsProvider>();
            var tenant = await tenantsProvider.FindByIdAsync(tenantIdString);
            tenantAccessor.Tenant = tenant;
        }

        await next.Send(context);
    }
}