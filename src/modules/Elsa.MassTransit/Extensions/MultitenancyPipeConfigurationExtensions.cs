using Elsa.Common.Multitenancy;
using Elsa.MassTransit.Middleware;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MassTransit.Extensions;

public static class MultitenancyPipeConfigurationExtensions
{
    public static void ConfigureTenantMiddleware(this IBusFactoryConfigurator bus, IBusRegistrationContext context)
    {
        var tenantAccessor = context.GetRequiredService<ITenantAccessor>();
        bus.ConfigureSend(pipe => pipe.UseTenantSendMiddleware(tenantAccessor));
        bus.ConfigurePublish(pipe => pipe.UseTenantPublishMiddleware(tenantAccessor));
        bus.UseConsumeFilter(typeof(TenantConsumeMiddleware<>), context);
    }
}