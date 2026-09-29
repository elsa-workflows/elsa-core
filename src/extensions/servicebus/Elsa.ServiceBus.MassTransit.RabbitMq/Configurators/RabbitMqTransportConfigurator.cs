using Elsa.Common;
using Elsa.ServiceBus.MassTransit.Contracts;
using Elsa.ServiceBus.MassTransit.Extensions;
using Elsa.ServiceBus.MassTransit.Options;
using Elsa.ServiceBus.MassTransit.RabbitMq.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.ServiceBus.MassTransit.RabbitMq.Configurators;

/// <summary>
/// <see cref="IBusTransportConfigurator"/> that configures MassTransit to use RabbitMQ.
/// Registered by <c>MassTransitRabbitMqShellFeature</c> and invoked by
/// <c>MassTransitFeature.PostConfigureServices</c> inside the single
/// <c>AddMassTransit</c> call.
/// </summary>
internal sealed class RabbitMqTransportConfigurator : IBusTransportConfigurator
{
    public void Configure(IBusRegistrationConfigurator bus)
    {
        bus.UsingRabbitMq((context, cfg) =>
        {
            var rabbitOptions = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            var busOptions = context.GetRequiredService<IOptions<MassTransitOptions>>().Value;

            cfg.Host(rabbitOptions.GetHostAddress(), h =>
            {
                if (string.IsNullOrWhiteSpace(rabbitOptions.ConnectionString))
                {
                    h.Username(rabbitOptions.UserName);
                    h.Password(rabbitOptions.Password);
                }
            });

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
