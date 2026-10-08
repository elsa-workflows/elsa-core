using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Http.Webhooks.ActivityProviders;
using Elsa.Workflows.Management.Extensions;
using Microsoft.Extensions.DependencyInjection;
using WebhooksCore;

namespace Elsa.Http.Webhooks.ShellFeatures;

/// <summary>
/// Installs and configures webhook services.
/// </summary>
[ShellFeature(DisplayName = "Webhooks", Description = "Provides support for sending and receiving webhooks")]
public class WebhooksFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddVariableTypeAndAlias<WebhookEvent>("WebhookEvent", "Webhooks");

        services
            .AddWebhooksCore(ConfigureHttpClient)
            .AddActivityProvider<WebhookEventActivityProvider>()
            .AddNotificationHandlersFrom<WebhooksFeature>();
    }

    private void ConfigureHttpClient(IHttpClientBuilder httpClient)
    {
        // TODO: Configure HTTP client with configurable parameters and resilience.
        httpClient.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            EnableMultipleHttp2Connections = true
        });
    }
}
