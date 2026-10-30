using Elsa.Features.Services;
using Elsa.ServiceBus.MassTransit.Features;
using Elsa.ServiceBus.MassTransit.RabbitMq.Features;

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
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, string connectionString)
    {
        feature.Module.Configure((Action<RabbitMqServiceBusFeature>)Configure);
        return feature;

        void Configure(RabbitMqServiceBusFeature bus)
        {
            bus.ConnectionString = connectionString;
        }
    }

    /// <summary>
    /// Enable and configure the RabbitMQ transport for MassTransit.
    /// </summary>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, Action<RabbitMqServiceBusFeature> configure)
    {
        feature.Module.Configure(configure);
        return feature;
    }

    /// <summary>
    /// Enable and configure the RabbitMQ transport for MassTransit.
    /// </summary>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, string connectionString, Action<RabbitMqServiceBusFeature> configure)
    {
        feature.Module.Configure<RabbitMqServiceBusFeature>(rabbitMqFeature =>
        {
            rabbitMqFeature.ConnectionString = connectionString;
            configure(rabbitMqFeature);
        });
        return feature;
    }
}