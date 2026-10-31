using Elsa.Common;
using Elsa.ServiceBus.MassTransit.Contracts;
using Elsa.ServiceBus.MassTransit.Extensions;
using Elsa.ServiceBus.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.ServiceBus.MassTransit.Configurators;

/// <summary>
/// Default <see cref="IBusTransportConfigurator"/> that uses the MassTransit in-memory transport.
/// Suitable for single-node development and testing scenarios.
/// Replaced at runtime by a transport-specific feature such as
/// <c>MassTransitRabbitMqShellFeature</c>.
/// </summary>
internal sealed class InMemoryTransportConfigurator : IBusTransportConfigurator
{
    public void Configure(IBusRegistrationConfigurator bus)
    {
        bus.UsingInMemory((context, cfg) =>
        {
            var busOptions = context.GetRequiredService<IOptions<MassTransitOptions>>().Value;

            cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));

            cfg.ConfigureJsonSerializerOptions(serializerOptions =>
            {
                var serializer = context.GetRequiredService<IJsonSerializer>();
                serializer.ApplyOptions(serializerOptions);
                return serializerOptions;
            });

            if (busOptions.PrefetchCount.HasValue)
                cfg.PrefetchCount = busOptions.PrefetchCount.Value;

            cfg.ConfigureTenantMiddleware(context);
        });
    }
}

