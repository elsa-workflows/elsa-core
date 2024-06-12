using System.ComponentModel;
using System.Reflection;
using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Contracts;
using Elsa.MassTransit.Extensions;
using Elsa.MassTransit.Formatters;
using Elsa.MassTransit.Models;
using Elsa.MassTransit.Options;
using Elsa.MassTransit.Services;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.MassTransit.Features;

/// <summary>
/// Enables the <see cref="MassTransitFeature"/> feature.
/// </summary>
public class MassTransitFeature : FeatureBase
{
    private bool _runInMemory;

    /// <inheritdoc />
    public MassTransitFeature(IModule module) : base(module)
    {
    }

    /// The number of messages to prefetch.
    [Obsolete("PrefetchCount has been moved to be included in MassTransitOptions")]
    public int? PrefetchCount { get; set; }
    
    public bool DisableConsumers { get; set; }

    /// <summary>
    /// A delegate that can be set to configure MassTransit's <see cref="IBusRegistrationConfigurator"/>. Used by transport-level features such as AzureServiceBusFeature and RabbitMqServiceBusFeature. 
    /// </summary>
    public Action<IBusRegistrationConfigurator>? BusConfigurator { get; set; }

    /// <summary>
    /// A factory that creates a <see cref="IEndpointChannelFormatter"/>.
    /// </summary>
    public Func<IServiceProvider, IEndpointChannelFormatter> ChannelQueueFormatterFactory { get; set; } = _ => new DefaultEndpointChannelFormatter();

    /// <inheritdoc />
    public override void Configure()
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var messageTypes = this.GetMessages();

        Services.AddSingleton(ChannelQueueFormatterFactory);
        Services.Configure<MassTransitOptions>(x => x.PrefetchCount ??= PrefetchCount);
        Services.Configure<MassTransitWorkflowDispatcherOptions>(x => { });
        Services.AddActivityProvider<MassTransitActivityTypeProvider>();
        _runInMemory = BusConfigurator is null;
        var busConfigurator = BusConfigurator ??= ConfigureInMemoryTransport;
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
            .Where(c => !c.IsTemporary || _runInMemory)
            .Concat(workflowMessageConsumers)
            .ToArray();

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

    private void ConfigureInMemoryTransport(IBusRegistrationConfigurator configure)
    {
        var consumers = this.GetConsumers().ToList();
        var temporaryConsumers = consumers
            .Where(c => c.IsTemporary)
            .ToList();

        // Consumers need to be added before the UsingInMemory statement to prevent exceptions.
        foreach (var consumer in temporaryConsumers)
            configure.AddConsumer(consumer.ConsumerType).ExcludeFromConfigureEndpoints();

        configure.UsingInMemory((context, busFactoryConfigurator) =>
        {
            var options = context.GetRequiredService<IOptions<MassTransitWorkflowDispatcherOptions>>().Value;

            if(!DisableConsumers)
            {
                foreach (var consumer in temporaryConsumers)
                {
                    busFactoryConfigurator.ReceiveEndpoint(consumer.Name!, endpoint =>
                    {
                        endpoint.ConcurrentMessageLimit = options.ConcurrentMessageLimit;
                        endpoint.ConfigureConsumer(context, consumer.ConsumerType);
                    });
                }
                
                busFactoryConfigurator.SetupWorkflowDispatcherEndpoints(context);
                busFactoryConfigurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));
            }
            
            busFactoryConfigurator.ConfigureJsonSerializerOptions(serializerOptions =>
            {
                var serializer = context.GetRequiredService<IJsonSerializer>();
                serializer.ApplyOptions(serializerOptions);
                return serializerOptions;
            });

            if (PrefetchCount != null) busFactoryConfigurator.PrefetchCount = PrefetchCount.Value;
        });
    }
}