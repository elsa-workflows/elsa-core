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
    
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, string connectionString, RabbitMqOptions options)
    {
        void Configure(RabbitMqServiceBusFeature bus)
        {
            bus.ConnectionString = connectionString;
            bus.Options = options;
        }

        feature.Module.Configure((Action<RabbitMqServiceBusFeature>) Configure);
        return feature;
    }
}