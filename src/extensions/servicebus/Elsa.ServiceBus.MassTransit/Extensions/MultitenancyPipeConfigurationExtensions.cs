using Elsa.Common.Multitenancy;
using Elsa.ServiceBus.MassTransit.Middleware;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.MassTransit.Extensions;

public static class MultitenancyPipeConfigurationExtensions
{
    public static void ConfigureTenantMiddleware(this IBusFactoryConfigurator bus, IBusRegistrationContext context)
    {
        // Multitenancy is optional for a standalone bus; without it there is no tenant to carry or restore.
        var tenantAccessor = context.GetService<ITenantAccessor>();
        if (tenantAccessor == null)
            return;

        bus.ConfigureSend(pipe => pipe.UseTenantSendMiddleware(tenantAccessor));
        bus.ConfigurePublish(pipe => pipe.UseTenantPublishMiddleware(tenantAccessor));
        bus.UseConsumeFilter(typeof(TenantConsumeMiddleware<>), context);
    }
}