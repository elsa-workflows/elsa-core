using Elsa.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using WebhooksCore;
using WebhooksCore.Options;

namespace Elsa.Http.Webhooks;

[UsedImplicitly]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the specified webhook with <see cref="WebhookSinksOptions"/>
    /// </summary>
    public static IServiceCollection RegisterWebhookSink(this IServiceCollection services, Uri endpoint)
    {
        var sink = new WebhookSink
        {
            Id = endpoint.ToString(),
            Url = endpoint
        };
        return services.RegisterSink(sink);
    }
    
    /// <summary>
    /// Registers the specified webhook with <see cref="WebhookSinksOptions"/>
    /// </summary>
    public static IServiceCollection RegisterSink(this IServiceCollection services, WebhookSink sink) => RegisterSinks(services, sink);
    
    /// <summary>
    /// Registers the specified webhooks with <see cref="WebhookSinksOptions"/>
    /// </summary>
    public static IServiceCollection RegisterSinks(this IServiceCollection services, params WebhookSink[] sinks)
    {
        services.Configure<WebhookSinksOptions>(options => options.Sinks.AddRange(sinks));
        return services;
    }
    
    /// <summary>
    /// Registers the specified webhook source with <see cref="WebhookSourcesOptions"/>
    /// </summary>
    public static IServiceCollection RegisterWebhookSource(this IServiceCollection services, WebhookSource source) => services.RegisterWebhookSources(source);
    
    /// <summary>
    /// Registers the specified webhook sources with <see cref="WebhookSourcesOptions"/>
    /// </summary>
    public static IServiceCollection RegisterWebhookSources(this IServiceCollection services, params WebhookSource[] sources)
    {
        services.Configure<WebhookSourcesOptions>(options => options.Sources.AddRange(sources));
        return services;
    }
}
