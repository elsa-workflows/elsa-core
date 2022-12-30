using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using Elsa.MassTransit.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IModule UseMassTransit(this IModule module, Action<MassTransitFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
    
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, Action<RabbitMqServiceBusFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}