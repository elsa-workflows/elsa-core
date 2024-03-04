using System.ComponentModel;
using System.Reflection;
using Elsa.Common.Contracts;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Extensions;
using Elsa.MassTransit.Models;
using Elsa.MassTransit.Options;
using Elsa.MassTransit.Services;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Serialization.Converters;
using MassTransit;
using MassTransit.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.MassTransit.Features;

/// <summary>
/// Enables the <see cref="MassTransitFeature"/> feature.
/// </summary>
public class MassTransitFeature : FeatureBase
{
    /// <inheritdoc />
    public MassTransitFeature(IModule module) : base(module)
    {
    }

    /// The number of messages to prefetch.
    public int? PrefetchCount { get; set; }

    /// <summary>
    /// A delegate that can be set to configure MassTransit's <see cref="IBusRegistrationConfigurator"/>. Used by transport-level features such as AzureServiceBusFeature and RabbitMqServiceBusFeature. 
    /// </summary>
    public Action<IBusRegistrationConfigurator>? BusConfigurator { get; set; }

    /// <inheritdoc />
    public override void Configure()
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var messageTypes = this.GetMessages();

        Services.Configure<MassTransitWorkflowDispatcherOptions>(x => { });
        Services.AddActivityProvider<MassTransitActivityTypeProvider>();

        void Configurator(IBusRegistrationConfigurator configure)
        {
            var consumers = this.GetConsumers().ToList();
            var temporaryConsumers = consumers
                .Where(c => c.IsTemporary)
                .ToList();

            configure.UsingInMemory((context, busFactoryConfigurator) =>
            {
                var options = context.GetRequiredService<IOptions<MassTransitWorkflowDispatcherOptions>>().Value;

                foreach (var consumer in temporaryConsumers)
                {
                    busFactoryConfigurator.ReceiveEndpoint(consumer.Name!, endpoint =>
                    {
                        endpoint.ConcurrentMessageLimit = options.ConcurrentMessageLimit;
                        endpoint.ConfigureConsumer<DispatchCancelWorkflowsRequestConsumer>(context);
                    });
                }

                busFactoryConfigurator.SetupWorkflowDispatcherEndpoints(context);
                busFactoryConfigurator.ConfigureEndpoints(context);
                busFactoryConfigurator.ConfigureJsonSerializerOptions(serializerOptions =>
                {
                    var serializer = context.GetRequiredService<IJsonSerializer>();
                    serializer.ApplyOptions(serializerOptions);
                    return serializerOptions;
                });

                if (PrefetchCount != null) busFactoryConfigurator.PrefetchCount = PrefetchCount.Value;
            });
        }

        var busConfigurator = BusConfigurator ??= Configurator;
        AddMassTransit(busConfigurator);

        // Add collected message types to options.
        Services.Configure<MassTransitActivityOptions>(
            options => options.MessageTypes = new HashSet<Type>(messageTypes));

        // Add collected message types as available variable types.
        Services.Configure<ManagementOptions>(options =>
        {
            foreach (var messageType in messageTypes)
            {
                var activityAttr = messageType.GetCustomAttribute<ActivityAttribute>();
                var categoryAttr = messageType.GetCustomAttribute<CategoryAttribute>();
                var category = categoryAttr?.Category ?? activityAttr?.Category ?? "MassTransit";
                var descriptionAttr = messageType.GetCustomAttribute<DescriptionAttribute>();
                var description = descriptionAttr?.Description ?? activityAttr?.Description;
                options.VariableDescriptors.Add(new VariableDescriptor(messageType, category, description));
            }
        });
    }

    /// <summary>
    /// Adds MassTransit to the service container and registers all collected assemblies for discovery of consumers.
    /// </summary>
    private void AddMassTransit(Action<IBusRegistrationConfigurator> busConfigurator)
    {
        // For each message type, create a concrete WorkflowMessageConsumer<T>.
        var workflowMessageConsumerType = typeof(WorkflowMessageConsumer<>);
        var workflowMessageConsumers = this.GetMessages()
            .Select(x => new ConsumerTypeDefinition(workflowMessageConsumerType.MakeGenericType(x)));

        // Concatenate the manually registered consumers with the workflow message consumers.
        var consumerTypeDefinitions = this.GetConsumers()
            // Temporary queues require implementation specific variables which will be handled in their respective projects.
            //.Where(c => c.IsTemporary == false)
            .Concat(workflowMessageConsumers).ToArray();

        Services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            foreach (var definition in consumerTypeDefinitions)
                bus.AddConsumer(definition.ConsumerType, definition.ConsumerDefinitionType);

            busConfigurator(bus);
        });

        Services.AddOptions<MassTransitHostOptions>().Configure(options =>
        {
            // Wait until the bus is started before returning from IHostedService.StartAsync.
            options.WaitUntilStarted = true;
        });
    }
}