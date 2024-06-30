using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Webhooks.ActivityProviders;
using Microsoft.Extensions.DependencyInjection;
using WebhooksCore;
using WebhooksCore.Options;

namespace Elsa.Webhooks.Features;

/// Installs and configures services that let the user register webhook endpoints.
public class WebhooksFeature : FeatureBase
{
    /// <inheritdoc />
    public WebhooksFeature(IModule module) : base(module)
    {
    }
    
    /// Registers the specified webhook with <see cref="WebhookSinksOptions"/>
    public WebhooksFeature RegisterWebhookSink(WebhookSink registration) => RegisterWebhookSinks(registration);
    
    /// Registers the specified webhook sinks with <see cref="WebhookSinksOptions"/>
    public WebhooksFeature RegisterWebhookSinks(params WebhookSink[] sinks)
    {
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
        Module.AddVariableTypeAndAlias<WebhookEvent>("WebhookEvent", "Webhooks");
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddWebhooksCore()
            .AddActivityProvider<WebhookEventActivityProvider>();
    }
}