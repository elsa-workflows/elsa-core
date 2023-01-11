using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using Elsa.MassTransit.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to <see cref="IModule"/> that enables and configures MassTransit.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Enable and configure MassTransit.
    /// </summary>
    public static IModule UseMassTransit(this IModule module, Action<MassTransitFeature>? configure = default) => module.Use(configure);

    /// <summary>
    /// Enable and configure the RabbitMQ broker for MassTransit.
    /// </summary>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, string connectionString) => feature.UseRabbitMq(new Uri(connectionString), null);

    /// <summary>
    /// Enable and configure the RabbitMQ broker for MassTransit.
    /// </summary>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, Uri connectionString) => feature.UseRabbitMq(connectionString, null);
    
    /// <summary>
    /// Enable and configure the RabbitMQ broker for MassTransit.
    /// </summary>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, RabbitMqOptions options) => feature.UseRabbitMq(null, options);

    /// <summary>
    /// Enable and configure the RabbitMQ broker for MassTransit.
    /// </summary>
    private static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, Uri? connectionString, RabbitMqOptions? options)
    {
        void Configure(RabbitMqServiceBusFeature bus)
        {
            bus.ConnectionString = connectionString;
            bus.Options = options;
        }

        feature.Module.Configure((Action<RabbitMqServiceBusFeature>) Configure);
        return feature;
    }
}