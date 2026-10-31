using Elsa.Common.Multitenancy;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using Proto.DependencyInjection;

namespace Elsa.Actors.ProtoActor.Middleware;

public static class TenantScopeMiddleware
{
    public static Props WithMultitenancy(this Props props, IServiceProvider sp)
    {
        props = props.WithReceiverMiddleware(next => ReadTenant(next, sp));
        props = props.WithSenderMiddleware(next => PropagateTenant(next, sp));
        return props;
    }
    
    public static Receiver ReadTenant(this Receiver next, IServiceProvider sp)
    {
        async Task Receiver(IReceiverContext context, MessageEnvelope envelope)
        {
            var tenantId = envelope.Header.GetValueOrDefault(HeaderNames.TenantId);
            var tenantFinder = tenantId != null ? sp.GetService<ITenantFinder>() : null;
            if (tenantFinder != null)
            {
                var tenant = await tenantFinder.FindByIdAsync(tenantId);
                var tenantScopeFactory = sp.GetRequiredService<ITenantScopeFactory>();
                await using var tenantScope = tenantScopeFactory.CreateScope(tenant);
                var originalServiceProvider = sp;
                context.System.WithServiceProvider(tenantScope.ServiceProvider);
                await next(context, envelope);
                context.System.WithServiceProvider(originalServiceProvider);
            }
            else
            {
                await next(context, envelope);
            }
        }

        return Receiver;
    }
    
    public static Sender PropagateTenant(this Sender next, IServiceProvider sp)
    {
        async Task Sender(ISenderContext context, PID target, MessageEnvelope envelope)
        {
            // Multitenancy is optional for a standalone actor host; without it there is no tenant to propagate.
            var tenant = sp.GetService<ITenantAccessor>()?.Tenant;
            if (tenant != null)
            {
                envelope.WithHeader(HeaderNames.TenantId, tenant.Id);
            }
            await next(context, target, envelope);
        }

        return Sender;
    }
}