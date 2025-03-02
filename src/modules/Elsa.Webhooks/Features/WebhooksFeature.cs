using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Webhooks.ActivityProviders;
using Microsoft.Extensions.DependencyInjection;
using WebhooksCore;
using WebhooksCore.Options;

namespace Elsa.Webhooks.Features;

/// <summary>
/// Installs and configures webhook services.
/// </summary>
public class WebhooksFeature(IModule module) : FeatureBase(module)
{
    public Action<WebhookSinksOptions> ConfigureSinks { get; set; } = _ => { };
    public Action<WebhookSourcesOptions> ConfigureSources { get; set; } = _ => { };
    public Action<IHttpClientBuilder> ConfigureHttpClient { get; set; } = _ => { };

    /// <summary>
    /// Registers the specified webhook with <see cref="WebhookSinksOptions"/>
    /// </summary>
    public WebhooksFeature RegisterWebhookSink(Uri endpoint)
    {
        var sink = new WebhookSink
        {
            Id = endpoint.ToString(),
            Url = endpoint
        };
        return RegisterSink(sink);
    }
    
    /// <summary>
    /// Registers the specified webhook with <see cref="WebhookSinksOptions"/>
    /// </summary>
    public WebhooksFeature RegisterSink(WebhookSink sink) => RegisterSinks(sink);
    
    /// <summary>
    /// Registers the specified webhooks with <see cref="WebhookSinksOptions"/>
    /// </summary>
    public WebhooksFeature RegisterSinks(params WebhookSink[] sinks)
    {
        ConfigureSinks += options => options.Sinks.AddRange(sinks);
        return this;
    }
    
    /// <summary>
    /// Registers the specified webhook source with <see cref="WebhookSourcesOptions"/>
    /// </summary>
    public WebhooksFeature RegisterWebhookSource(WebhookSource source) => RegisterWebhookSources(source);
    
    /// <summary>
    /// Registers the specified webhook sources with <see cref="WebhookSourcesOptions"/>
    /// </summary>
    public WebhooksFeature RegisterWebhookSources(params WebhookSource[] sources)
    {
        ConfigureSources += options => options.Sources.AddRange(sources);
        return this;
    }

    public override void Configure()
    {
        Module
            .AddVariableTypeAndAlias<WebhookEvent>("WebhookEvent", "Webhooks")
            .AddFastEndpointsAssembly(GetType());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(ConfigureSinks);
        Services.Configure(ConfigureSources);
        
        Services
            .AddWebhooksCore(ConfigureHttpClient)
            .AddActivityProvider<WebhookEventActivityProvider>()
            .AddNotificationHandlersFrom<WebhooksFeature>();
    }
}