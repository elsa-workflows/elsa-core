using Elsa.Features.Services;
using Elsa.Webhooks.Features;

namespace Elsa.Webhooks.Extensions;

/// <summary>
/// Adds extensions to <see cref="IModule"/> that enables the <see cref="WebhooksFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Enables and configures the <see cref="WebhooksFeature"/> feature.
    /// </summary>
    public static IModule UseWebhooks(this IModule module, Action<WebhooksFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}