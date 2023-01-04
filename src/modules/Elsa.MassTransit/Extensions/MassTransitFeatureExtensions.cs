using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using MassTransit;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to <see cref="IModule"/> that enables and configures MassTransit.
/// </summary>
public static class MassTransitFeatureExtensions
{
    private static readonly object ServiceBusConsumerTypesKey = new();

    /// <summary>
    /// Registers the specified type for MassTransit service bus consumer discovery.
    /// </summary>
    public static MassTransitFeature AddConsumer<T>(this MassTransitFeature feature) where T : IConsumer => feature.AddConsumer(typeof(T));
    
    /// <summary>
    /// Registers the specified type for MassTransit service bus consumer discovery.
    /// </summary>
    public static MassTransitFeature AddConsumer(this MassTransitFeature feature, Type type)
    {
        var types = feature.Module.Properties.GetOrAdd(ServiceBusConsumerTypesKey, () => new HashSet<Type>());
        types.Add(type);
        return feature;
    }
    
    /// <summary>
    /// Returns all collected types for discovery of service bus consumers.
    /// </summary>
    internal static IEnumerable<Type> GetConsumers(this MassTransitFeature feature) => feature.Module.Properties.GetOrAdd(ServiceBusConsumerTypesKey, () => new HashSet<Type>());
}