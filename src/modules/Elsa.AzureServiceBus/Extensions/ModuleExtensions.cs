using Elsa.AzureServiceBus.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="IModule"/> to register Azure Service Bus related services.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Enable and configure the <see cref="AzureServiceBusFeature"/> feature. 
    /// </summary>
    public static IModule UseAzureServiceBus(this IModule module, string connectionStringOrName, Action<AzureServiceBusFeature>? setup = default)
    {
        setup += feature => feature.AzureServiceBusOptions += options => options.ConnectionStringOrName = connectionStringOrName;
        return module.Use(setup);
    }

    /// <summary>
    /// Enable and configure the <see cref="AzureServiceBusFeature"/> feature. 
    /// </summary>
    public static IModule UseAzureServiceBus(this IModule module, Action<AzureServiceBusFeature> setup) => module.Use(setup);
}