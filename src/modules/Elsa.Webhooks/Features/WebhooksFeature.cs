using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Webhooks.ActivityProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebhooksCore;
using WebhooksCore.Options;

namespace Elsa.Webhooks.Features;

/// <summary>
/// Installs and configures webhook services.
/// </summary>
public class WebhooksFeature : FeatureBase
{
    /// <inheritdoc />
    public WebhooksFeature(IModule module) : base(module)
    {
    }

    public Action<IOptions<WebhookSinksOptions>> ConfigureSinks { get; set; } = options => { };
    public Action<IHttpClientBuilder> ConfigureHttpClient { get; set; } = builder => { };

    /// Registers the specified webhook with <see cref="WebhookSinksOptions"/>
    public WebhooksFeature RegisterWebhookSink(Uri endpoint)
    {
        var sink = new WebhookSink
        {
            Id = endpoint.ToString(),
            Url = endpoint
        };
        return RegisterSink(sink);
    }
    
    /// Registers the specified webhook with <see cref="WebhookSinksOptions"/>
    public WebhooksFeature RegisterSink(WebhookSink sink) => RegisterSinks(sink);
    
    /// Registers the specified webhooks with <see cref="WebhookSinksOptions"/>
    public WebhooksFeature RegisterSinks(params WebhookSink[] sinks)
    {
        Services.Configure(ConfigureSinks);
        Services.Configure<WebhookSinksOptions>(options => options.Sinks.AddRange(sinks));
        return this;
    }
    
    /// Registers the specified webhook source with <see cref="WebhookSourcesOptions"/>
    public WebhooksFeature RegisterWebhookSource(WebhookSource source) => RegisterWebhookSources(source);
    
    /// Registers the specified webhook sources with <see cref="WebhookSourcesOptions"/>
    public WebhooksFeature RegisterWebhookSources(params WebhookSource[] sources)
    {
        Services.Configure<WebhookSourcesOptions>(options => options.Sources.AddRange(sources));
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
        Services
            .AddWebhooksCore(ConfigureHttpClient)
            .AddActivityProvider<WebhookEventActivityProvider>();
    }
}