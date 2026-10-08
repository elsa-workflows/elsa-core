using Elsa.Studio.Contracts;
using Elsa.Studio.Http.Webhooks.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Http.Webhooks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebhooksModule(this IServiceCollection services)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, WebhooksMenu>();
    }
}
