using CShells.Features;
using Elsa.ServiceBus.MassTransit.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.MassTransit.Extensions;

/// <summary>
/// Extension methods on <see cref="IServiceCollection"/> for registering MassTransit consumers
/// from CShells shell features.
/// </summary>
public static class MassTransitConsumerExtensions
{
    private static readonly object ConsumersKey = new();

    /// <summary>
    /// Registers a MassTransit consumer via the shared <see cref="ShellFeatureContext"/>.
    /// </summary>
    public static ShellFeatureContext AddMassTransitConsumer<TConsumer>(
        this ShellFeatureContext context,
        string? endpointName = null,
        bool isTemporary = false,
        bool ignoreConsumersDisabled = false)
        where TConsumer : class
    {
        var consumers = context.GetOrAdd(ConsumersKey, () => new List<ConsumerTypeDefinition>());
        consumers.Add(new ConsumerTypeDefinition(
            typeof(TConsumer),
            Name: endpointName,
            IsTemporary: isTemporary,
            IgnoreConsumersDisabled: ignoreConsumersDisabled));
        return context;
    }

    /// <summary>
    /// Registers a MassTransit consumer with a consumer definition via the shared
    /// <see cref="ShellFeatureContext"/>.
    /// </summary>
    public static ShellFeatureContext AddMassTransitConsumer<TConsumer, TDefinition>(
        this ShellFeatureContext context,
        string? endpointName = null,
        bool isTemporary = false,
        bool ignoreConsumersDisabled = false)
        where TConsumer : class
        where TDefinition : class
    {
        var consumers = context.GetOrAdd(ConsumersKey, () => new List<ConsumerTypeDefinition>());
        consumers.Add(new ConsumerTypeDefinition(
            typeof(TConsumer),
            ConsumerDefinitionType: typeof(TDefinition),
            Name: endpointName,
            IsTemporary: isTemporary,
            IgnoreConsumersDisabled: ignoreConsumersDisabled));
        return context;
    }

    /// <summary>
    /// Returns all consumer definitions registered via <see cref="AddMassTransitConsumer{TConsumer}"/>.
    /// </summary>
    internal static IReadOnlyList<ConsumerTypeDefinition> GetConsumers(this ShellFeatureContext context)
    {
        return context.Properties.TryGetValue(ConsumersKey, out var value)
            ? (List<ConsumerTypeDefinition>)value
            : [];
    }
}

