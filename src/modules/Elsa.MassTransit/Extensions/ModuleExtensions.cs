using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using MassTransit;

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
    /// Registers the specified consumer with MassTransit.
    /// </summary>
    public static IModule AddMassTransitConsumer<T>(this IModule module, string? name = null, bool isTemporary = false, bool ignoreConsumersDisabled = false) where T : IConsumer
    {
        module.Configure<MassTransitFeature>(massTransit => massTransit.AddConsumer<T>(name, isTemporary, ignoreConsumersDisabled));
        return module;
    }
    
    /// <summary>
    /// Registers the specified consumer and consumer definition with MassTransit.
    /// </summary>
    public static IModule AddMassTransitConsumer<T, TDefinition>(this IModule module, string? name = null, bool isTemporary = false, bool ignoreConsumersDisabled = false) 
        where T : IConsumer 
        where TDefinition : IConsumerDefinition
    {
        module.Configure<MassTransitFeature>(massTransit => massTransit.AddConsumer<T, TDefinition>(name, isTemporary, ignoreConsumersDisabled));
        return module;
    }
}