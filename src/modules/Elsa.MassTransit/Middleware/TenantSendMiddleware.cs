using Elsa.Common.Multitenancy;
using MassTransit;

namespace Elsa.MassTransit.Middleware;

public class TenantSendMiddleware(ITenantAccessor tenantAccessor) : IFilter<SendContext>
{
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("tenantSend");
    }

    public async Task Send(SendContext context, IPipe<SendContext> next)
    {
        var tenantId = tenantAccessor.Tenant?.Id;

        if (!string.IsNullOrEmpty(tenantId))
            context.Headers.Set("TenantId", tenantId);

        await next.Send(context);
    }
}