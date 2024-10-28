using Elsa.Common.Multitenancy;
using MassTransit;

namespace Elsa.MassTransit.Middleware;

public class TenantConsumeMiddleware<T>(ITenantContextInitializer tenantContextInitializer) : IFilter<ConsumeContext<T>> where T : class
{
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("tenantConsume");
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        if (context.Headers.TryGetHeader(HeaderNames.TenantId, out var tenantId) && tenantId is string tenantIdString) 
            await tenantContextInitializer.InitializeAsync(tenantIdString, context.CancellationToken);

        await next.Send(context);
    }
}