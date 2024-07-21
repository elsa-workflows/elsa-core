using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebhooksCore;
using WebhooksCore.Options;

namespace Elsa.Webhooks.Features;

/// Installs and configures webhook services.
public class WebhooksFeature : FeatureBase
{
    /// <inheritdoc />
    public WebhooksFeature(IModule module) : base(module)
    {
    }

    public Action<IOptions<WebhookSinksOptions>> ConfigureSinks { get; set; } = options => { };

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

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddWebhooksCore();
    }
}