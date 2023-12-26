using System.ComponentModel;
using System.Reflection;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Implementations;
using Elsa.MassTransit.Models;
using Elsa.MassTransit.Options;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Serialization.Converters;
using MassTransit;
using MassTransit.Serialization;
using Microsoft.Extensions.DependencyInjection;

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

        var busConfigurator = BusConfigurator ??= configure =>
        {
            configure.UsingInMemory((context, configurator) =>
            {
                configurator.ConfigureEndpoints(context);

                if (PrefetchCount != null)
                    configurator.PrefetchCount = PrefetchCount.Value;
            });
        };
        AddMassTransit(busConfigurator);

        // Add collected message types to options.
        Services.Configure<MassTransitActivityOptions>(options => options.MessageTypes = new HashSet<Type>(messageTypes));

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

        // Configure message serializer.
        SystemTextJsonMessageSerializer.Options.Converters.Add(new TypeJsonConverter(WellKnownTypeRegistry.CreateDefault()));
    }

    /// <summary>
    /// Adds MassTransit to the service container and registers all collected assemblies for discovery of consumers.
    /// </summary>
    private void AddMassTransit(Action<IBusRegistrationConfigurator> busConfigurator)
    {
        // For each message type, create a concrete WorkflowMessageConsumer<T>.
        var workflowMessageConsumerType = typeof(WorkflowMessageConsumer<>);
        var workflowMessageConsumers = this.GetMessages().Select(x => new ConsumerTypeDefinition(workflowMessageConsumerType.MakeGenericType(x)));

        // Concatenate the manually registered consumers with the workflow message consumers.
        var consumerTypeDefinitions = this.GetConsumers().Concat(workflowMessageConsumers).ToArray();

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