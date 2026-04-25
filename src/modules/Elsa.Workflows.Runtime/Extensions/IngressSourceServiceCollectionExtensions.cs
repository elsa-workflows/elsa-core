using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Registration helpers for <see cref="IIngressSource"/> implementations.
/// </summary>
public static class IngressSourceServiceCollectionExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TSource"/> as an <see cref="IIngressSource"/> singleton. If the type also implements
    /// <see cref="IForceStoppable"/>, the force-stop capability is discovered automatically — no separate registration needed.
    /// </summary>
    public static IServiceCollection AddIngressSource<TSource>(
        this IServiceCollection services,
        Action<IngressSourceRegistrationOptions>? configure = null)
        where TSource : class, IIngressSource
    {
        if (configure is not null)
            services.Configure<IngressSourceRegistrationOptions>(typeof(TSource).FullName!, configure);

        services.AddSingleton<TSource>();
        services.AddSingleton<IIngressSource>(sp => sp.GetRequiredService<TSource>());
        return services;
    }
}
