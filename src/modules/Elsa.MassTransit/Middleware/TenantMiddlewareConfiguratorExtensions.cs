using Elsa.Common.Multitenancy;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Configuration;

namespace Elsa.MassTransit.Middleware;

[UsedImplicitly]
public static class TenantMiddlewareConfiguratorExtensions
{
    public static void UseTenantSendMiddleware(this ISendPipeConfigurator configurator, ITenantAccessor tenantAccessor)
    {
        configurator.AddPipeSpecification(new FilterPipeSpecification<SendContext>(new TenantSendMiddleware(tenantAccessor)));
    }
    
    public static void UseTenantPublishMiddleware(this IPublishPipeConfigurator configurator, ITenantAccessor tenantAccessor)
    {
        configurator.AddPipeSpecification(new FilterPipeSpecification<PublishContext>(new TenantPublishMiddleware(tenantAccessor)));
    }
}