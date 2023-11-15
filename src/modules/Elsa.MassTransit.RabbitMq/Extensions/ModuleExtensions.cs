using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using Elsa.MassTransit.Options;
using Elsa.MassTransit.RabbitMq.Features;
using Elsa.MassTransit.RabbitMq.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to <see cref="IModule"/> that enables and configures MassTransit and the RabbitMQ transport.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Enable and configure the RabbitMQ transport for MassTransit.
    /// </summary>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, string connectionString) => feature.UseRabbitMq(new Uri(connectionString), null);

    /// <summary>
    /// Enable and configure the RabbitMQ transport for MassTransit.
    /// </summary>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, Uri connectionString) => feature.UseRabbitMq(connectionString, null);
    
    /// <summary>
    /// Enable and configure the RabbitMQ transport for MassTransit.
    /// </summary>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, RabbitMqOptions options) => feature.UseRabbitMq(null, options);

    /// <summary>
    /// Enable and configure the RabbitMQ transport for MassTransit.
    /// </summary>
    private static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, Uri? connectionString, RabbitMqOptions? options)
    {
        feature.Module.Configure((Action<RabbitMqServiceBusFeature>) Configure);
        return feature;

        void Configure(RabbitMqServiceBusFeature bus)
        {
            bus.ConnectionString = connectionString;
            bus.Options = options;
        }
    }
}