using Elsa.Common;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Hosting.Management.Contracts;
using Elsa.Hosting.Management.Features;
using Elsa.MassTransit.Extensions;
using Elsa.MassTransit.Features;
using Elsa.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.MassTransit.RabbitMq.Features;

/// <summary>
/// Configures MassTransit to use the RabbitMQ transport.
/// </summary>
[DependsOn(typeof(MassTransitFeature))]
[DependsOn(typeof(ClusteringFeature))]
public class RabbitMqServiceBusFeature : FeatureBase
{
    /// <inheritdoc />
    public RabbitMqServiceBusFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A RabbitMQ connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Configures the RabbitMQ transport options.
    /// </summary>
    public Action<RabbitMqTransportOptions>? TransportOptions { get; set; }

    /// <summary>
    /// Configures the RabbitMQ bus.
    /// </summary>
    /// <remarks>This method is being marked as obsolete in favor of the ConfigureTransportBus which will provide additional access to the <see cref="IBusRegistrationContext"/></remarks>
    [Obsolete("Use ConfigureTransportBus instead which provides a reference to IBusRegistrationContext.")]
    public Action<IRabbitMqBusFactoryConfigurator>? ConfigureServiceBus { get; set; }

    /// <summary>
    /// Configures the RabbitMQ bus within MassTransit for additional transport level components or features.
    /// This action provides access to the <see cref="IBusRegistrationContext"/> and <see cref="IRabbitMqBusFactoryConfigurator"/>.
    /// </summary>
    /// <remarks>
    /// Use this action to configure advanced settings and features for the RabbitMQ bus, such as middleware 
    /// or additional endpoints. This action will run in addition to the Elsa required configuration.
    /// </remarks>
    public Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator> ConfigureTransportBus { get; set; }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<MassTransitFeature>(massTransitFeature =>
        {
            massTransitFeature.BusConfigurator = configure =>
            {
                var temporaryConsumers = massTransitFeature.GetConsumers()
                    .Where(c => c.IsTemporary)
                    .ToList();

                // Consumers need to be added before the UsingRabbitMq statement to prevent exceptions.
                foreach (var consumer in temporaryConsumers)
                    configure.AddConsumer(consumer.ConsumerType).ExcludeFromConfigureEndpoints();

                configure.UsingRabbitMq((context, configurator) =>
                {
                    var options = context.GetRequiredService<IOptions<MassTransitOptions>>().Value;
                    var instanceNameProvider = context.GetRequiredService<IApplicationInstanceNameProvider>();

                    if (!string.IsNullOrEmpty(ConnectionString))
                        configurator.Host(ConnectionString);

                    if (options.PrefetchCount is not null)
                        configurator.PrefetchCount = options.PrefetchCount.Value;
                    configurator.ConcurrentMessageLimit = options.ConcurrentMessageLimit;

                    ConfigureServiceBus?.Invoke(configurator);
                    ConfigureTransportBus?.Invoke(context, configurator);

                    foreach (var consumer in temporaryConsumers)
                    {
                        configurator.ReceiveEndpoint($"{instanceNameProvider.GetName()}-{consumer.Name}",
                            endpointConfigurator =>
                            {
                                endpointConfigurator.QueueExpiration = options.TemporaryQueueTtl ?? TimeSpan.FromHours(1);
                                endpointConfigurator.ConcurrentMessageLimit = options.ConcurrentMessageLimit;
                                endpointConfigurator.Durable = false;
                                endpointConfigurator.AutoDelete = true;
                                endpointConfigurator.ConfigureConsumer(context, consumer.ConsumerType);
                            });
                    }

                    if (!massTransitFeature.DisableConsumers)
                    {
                        if (Module.HasFeature<MassTransitWorkflowDispatcherFeature>())
                            configurator.SetupWorkflowDispatcherEndpoints(context);
                    }

                    configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));
                    
                    configurator.ConfigureJsonSerializerOptions(serializerOptions =>
                    {
                        var serializer = context.GetRequiredService<IJsonSerializer>();
                        serializer.ApplyOptions(serializerOptions);
                        return serializerOptions;
                    });
                    
                    configurator.ConfigureTenantMiddleware(context);
                });
            };
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        if (TransportOptions != null) Services.Configure(TransportOptions);
    }
}