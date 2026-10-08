using Elsa.Common;
using Elsa.Hosting.Management.Contracts;
using Elsa.ServiceBus.MassTransit.AzureServiceBus.Options;
using Elsa.ServiceBus.MassTransit.Contracts;
using Elsa.ServiceBus.MassTransit.Extensions;
using Elsa.ServiceBus.MassTransit.Models;
using Elsa.ServiceBus.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.ServiceBus.MassTransit.AzureServiceBus.Configurators;

/// <summary>
/// <see cref="IBusTransportConfigurator"/> that configures MassTransit to use Azure Service Bus.
/// Registered by <c>MassTransitAzureServiceBusShellFeature</c> and invoked by
/// <c>MassTransitFeature.PostConfigureServices</c> inside the single <c>AddMassTransit</c> call.
/// </summary>
internal sealed class AzureServiceBusTransportConfigurator : IBusTransportConfigurator
{
    public void Configure(IBusRegistrationConfigurator bus)
    {
        bus.AddServiceBusMessageScheduler();

        bus.UsingAzureServiceBus((context, cfg) =>
        {
            var asbOptions = context.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
            var busOptions = context.GetRequiredService<IOptions<MassTransitOptions>>().Value;

            // Resolve connection string — either a literal or a named connection string from IConfiguration.
            if (!string.IsNullOrWhiteSpace(asbOptions.ConnectionStringOrName))
            {
                var configuration = context.GetRequiredService<IConfiguration>();
                var connectionString = configuration.GetConnectionString(asbOptions.ConnectionStringOrName)
                                      ?? asbOptions.ConnectionStringOrName;
                cfg.Host(connectionString);
            }

            if (busOptions.PrefetchCount is not null)
                cfg.PrefetchCount = busOptions.PrefetchCount.Value;
            if (busOptions.MaxAutoRenewDuration is not null)
                cfg.MaxAutoRenewDuration = busOptions.MaxAutoRenewDuration.Value;
            cfg.ConcurrentMessageLimit = busOptions.ConcurrentMessageLimit;

            cfg.UseServiceBusMessageScheduler();

            // Wire up temporary consumers on their own named receive endpoints with
            // auto-delete-on-idle semantics, keyed by the application instance name.
            var instanceNameProvider = context.GetRequiredService<IApplicationInstanceNameProvider>();
            var consumers = context.GetRequiredService<IEnumerable<ConsumerTypeDefinition>>();

            foreach (var consumer in consumers.Where(c => c.IsTemporary))
            {
                var queueName = $"{instanceNameProvider.GetName()}-{consumer.Name}";
                cfg.ReceiveEndpoint(queueName, ep =>
                {
                    ep.AutoDeleteOnIdle = busOptions.TemporaryQueueTtl ?? TimeSpan.FromHours(1);
                    ep.ConcurrentMessageLimit = busOptions.ConcurrentMessageLimit;
                    ep.ConfigureConsumer(context, consumer.ConsumerType);
                });
            }

            cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));

            cfg.ConfigureJsonSerializerOptions(serializerOptions =>
            {
                var serializer = context.GetRequiredService<IJsonSerializer>();
                serializer.ApplyOptions(serializerOptions);
                return serializerOptions;
            });

            cfg.ConfigureTenantMiddleware(context);
        });
    }
}


