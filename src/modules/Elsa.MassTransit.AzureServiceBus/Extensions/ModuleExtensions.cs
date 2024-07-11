using Elsa.Features.Services;
using Elsa.MassTransit.AzureServiceBus.Features;
using Elsa.MassTransit.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to <see cref="IModule"/> that enables and configures MassTransit and the Azure Service Bus transport.
/// </summary>
[PublicAPI]
public static class ModuleExtensions
{
    /// <summary>
    /// Enable and configure the Azure Service Bus transport for MassTransit.
    /// </summary>
    public static MassTransitFeature UseAzureServiceBus(this MassTransitFeature feature, string? connectionString, Action<AzureServiceBusFeature>? configure = default)
    {
        feature.Module.Configure((Action<AzureServiceBusFeature>)Configure);
        return feature;

        void Configure(AzureServiceBusFeature bus)
        {
            bus.AzureServiceBusOptions = options => options.ConnectionStringOrName = connectionString;
            bus.ConnectionString = connectionString;
            configure?.Invoke(bus);
        }
    }
}