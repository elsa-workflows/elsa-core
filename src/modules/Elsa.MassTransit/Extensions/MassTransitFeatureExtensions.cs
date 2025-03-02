using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using Elsa.MassTransit.Models;
using Elsa.MassTransit.Services;
using MassTransit;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to <see cref="IModule"/> that enables and configures MassTransit.
/// </summary>
public static class MassTransitFeatureExtensions
{
    private static readonly object ServiceBusConsumerTypesKey = new();
    private static readonly object MessageTypesKey = new();

    /// <summary>
    /// Registers the specified type for MassTransit service bus consumer discovery.
    /// </summary>
    public static MassTransitFeature AddConsumer<T>(this MassTransitFeature feature, string? name, bool isTemporary, bool ignoreConsumersDisabled = false) where T : IConsumer =>
        feature.AddConsumer(typeof(T), name, isTemporary, ignoreConsumersDisabled: ignoreConsumersDisabled);

    /// <summary>
    /// Registers the specified type for MassTransit service bus consumer discovery.
    /// </summary>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <typeparam name="TDefinition">The consumer definition type.</typeparam>
    public static MassTransitFeature AddConsumer<T, TDefinition>(this MassTransitFeature feature, string? name, bool isTemporary, bool ignoreConsumersDisabled = false)
        where T : IConsumer
        where TDefinition : IConsumerDefinition
    {
        return feature.AddConsumer(typeof(T), name, isTemporary, typeof(TDefinition), ignoreConsumersDisabled);
    }

    /// <summary>
    /// Registers the specified type for MassTransit service bus consumer discovery.
    /// </summary>
    public static MassTransitFeature AddConsumer(this MassTransitFeature feature, Type type, string? name, bool isTemporary, Type? consumerDefinitionType = default, bool ignoreConsumersDisabled = false)
    {
        var types = feature.Module.Properties.GetOrAdd(ServiceBusConsumerTypesKey, () => new HashSet<ConsumerTypeDefinition>());
        types.Add(new ConsumerTypeDefinition(type, consumerDefinitionType, name, isTemporary, ignoreConsumersDisabled));
        return feature;
    }

    /// <summary>
    /// Registers a message type which is to be used by the <see cref="MassTransitActivityTypeProvider"/> to dynamically provide activities to send and receive these messages.
    /// </summary>
    public static MassTransitFeature AddMessageType<T>(this MassTransitFeature feature) where T : class => feature.AddMessageType(typeof(T));

    /// <summary>
    /// Registers a message type which is to be used by the <see cref="MassTransitActivityTypeProvider"/> to dynamically provide activities to send and receive these messages.
    /// </summary>
    public static MassTransitFeature AddMessageType(this MassTransitFeature feature, Type type)
    {
        var types = feature.Module.Properties.GetOrAdd(MessageTypesKey, () => new HashSet<Type>());
        types.Add(type);
        return feature;
    }

    /// <summary>
    /// Returns all collected consumer types.
    /// </summary>
    public static IEnumerable<ConsumerTypeDefinition> GetConsumers(this MassTransitFeature feature)
    {
        var definitions = feature.Module.Properties.GetOrAdd(ServiceBusConsumerTypesKey, () => new HashSet<ConsumerTypeDefinition>());
        var disableConsumers = feature.DisableConsumers;
        return !disableConsumers ? definitions : definitions.Where(x => x.IgnoreConsumersDisabled).ToList();
    }

    /// <summary>
    /// Returns all collected message types.
    /// </summary>
    internal static IEnumerable<Type> GetMessages(this MassTransitFeature feature) => feature.Module.Properties.GetOrAdd(MessageTypesKey, () => new HashSet<Type>());
}