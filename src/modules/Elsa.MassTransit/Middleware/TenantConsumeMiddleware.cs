using Elsa.Common.Multitenancy;
using MassTransit;
using MassTransit.DependencyInjection;

namespace Elsa.MassTransit.Middleware;

public class TenantConsumeMiddleware<T>(ITenantFinder tenantFinder, ITenantScopeFactory tenantScopeFactory, Bind<IBus, ISetScopedConsumeContext> scopeSetter) : IFilter<ConsumeContext<T>> where T : class
{
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("tenantConsume");
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        if (context.Headers.TryGetHeader(HeaderNames.TenantId, out var tenantId) && tenantId is string tenantIdString)
        {
            var tenant = await tenantFinder.FindByIdAsync(tenantIdString);
            await using var tenantScope = tenantScopeFactory.CreateScope(tenant);
            using var scope = scopeSetter.Value.PushContext(tenantScope.ServiceScope, context);
            await next.Send(context);
        }
        else
        {
            await next.Send(context);
        }
    }
}