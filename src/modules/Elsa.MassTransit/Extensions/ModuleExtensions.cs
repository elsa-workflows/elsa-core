using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using Elsa.MassTransit.Options;
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
    public static IModule AddMassTransitConsumer<T>(this IModule module) where T : IConsumer
    {
        module.Configure<MassTransitFeature>(massTransit => massTransit.AddConsumer<T>());
        return module;
    }
}