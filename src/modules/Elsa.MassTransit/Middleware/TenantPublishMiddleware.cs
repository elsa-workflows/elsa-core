using Elsa.Common.Multitenancy;
using MassTransit;

namespace Elsa.MassTransit.Middleware;

public class TenantPublishMiddleware(ITenantAccessor tenantAccessor) : IFilter<PublishContext>
{
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("tenantSend");
    }

    public async Task Send(PublishContext context, IPipe<PublishContext> next)
    {
        var tenantId = tenantAccessor.Tenant?.Id;

        if (!string.IsNullOrEmpty(tenantId))
            context.Headers.Set(HeaderNames.TenantId, tenantId);

        await next.Send(context);
    }
}