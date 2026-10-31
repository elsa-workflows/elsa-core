using System.ComponentModel;
using System.Reflection;
using CShells.Features;
using CShells.Lifecycle;
using Elsa.ServiceBus.MassTransit.Configurators;
using Elsa.ServiceBus.MassTransit.Contracts;
using Elsa.ServiceBus.MassTransit.Extensions;
using Elsa.ServiceBus.MassTransit.Formatters;
using Elsa.ServiceBus.MassTransit.Options;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.ServiceBus.MassTransit.ShellFeatures.Handlers;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Management.Options;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.MassTransit.ShellFeatures;

/// <summary>
/// Shell feature for enabling the MassTransit service bus.
/// </summary>
/// <remarks>
/// This feature owns the single <c>AddMassTransit</c> call for the shell, but defers it
/// to <see cref="PostConfigureServices"/> so that transport features which depend on this
/// feature (and therefore run after it) can register their own
/// <see cref="IBusTransportConfigurator"/> first.  The last-registered configurator wins.
/// <para>
/// Consumers are collected from all features via
/// <c>context.AddMassTransitConsumer&lt;T&gt;(…)</c> on the shared
/// <see cref="ShellFeatureContext"/> and are read back in
/// <see cref="PostConfigureServices"/>.
/// </para>
/// </remarks>
[ShellFeature(
    DisplayName = "MassTransit Service Bus",
    Description = "Enables MassTransit-based message publishing and handling for workflows")]
[UsedImplicitly]
public class MassTransitFeature(ShellFeatureContext context) : IShellFeature, IPostConfigureShellServices
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(DefaultEndpointChannelFormatter);
        services.Configure<MassTransitOptions>(_ => { });
        services.Configure<MassTransitWorkflowDispatcherOptions>(_ => { });
        services.AddActivityProvider<MassTransitActivityTypeProvider>();

        // Register the default in-memory transport.
        services.AddSingleton<IBusTransportConfigurator, InMemoryTransportConfigurator>();

        services.AddOptions<MassTransitHostOptions>().Configure(options =>
            options.WaitUntilStarted = true);

        var messageTypes = CollectMessageTypes().ToList();

        services.Configure<MassTransitActivityOptions>(options =>
            options.MessageTypes = new HashSet<Type>(messageTypes));

        services.Configure<ManagementOptions>(options =>
        {
            foreach (var messageType in messageTypes)
            {
                var activityAttr = messageType.GetCustomAttribute<ActivityAttribute>();
                var categoryAttr = messageType.GetCustomAttribute<CategoryAttribute>();
                var category = categoryAttr?.Category ?? activityAttr?.Category ?? "MassTransit";
                var descriptionAttr = messageType.GetCustomAttribute<DescriptionAttribute>();
                options.VariableDescriptors.Add(new(messageType, category, descriptionAttr?.Description));
            }
        });

        // Register shell lifecycle handler to start/stop the bus when shell activates/deactivates.
        // This is necessary because MassTransit's hosted service is registered in the shell's
        // service collection, but only hosted services in the root container are started by the host.
        services.AddTransient<IShellInitializer, MassTransitShellLifecycleHandler>();
        services.AddTransient<IDrainHandler, MassTransitShellLifecycleHandler>();
    }

    /// <inheritdoc />
    public void PostConfigureServices(IServiceCollection services)
    {
        // --- Resolve transport configurator ---
        var transportDescriptor = services
            .LastOrDefault(d => d.ServiceType == typeof(IBusTransportConfigurator));

        var transportConfigurator = transportDescriptor is not null
            ? (IBusTransportConfigurator)Activator.CreateInstance(transportDescriptor.ImplementationType!)!
            : new InMemoryTransportConfigurator();

        // --- Read accumulated consumer registrations from the shared context ---
        var consumers = context.GetConsumers();

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            foreach (var consumer in consumers)
                bus.AddConsumer(consumer.ConsumerType, consumer.ConsumerDefinitionType);

            transportConfigurator.Configure(bus);
        });
    }

    /// <summary>Override to provide message types exposed as workflow activities.</summary>
    protected virtual IEnumerable<Type> CollectMessageTypes() => [];

    private static IEndpointChannelFormatter DefaultEndpointChannelFormatter(IServiceProvider _) =>
        new DefaultEndpointChannelFormatter();
}
