using Elsa.Features.Services;
using Elsa.Workflows.Core;
using MassTransit;

namespace Elsa.MassTransit.Extensions;

public static class ModuleExtensions
{
    private static readonly object ServiceBusConsumerTypesKey = new();

    /// <summary>
    /// Registers the specified type for MassTransit service bus consumer discovery.
    /// </summary>
    public static void AddMassTransitServiceBusConsumerType(this IModule module, Type type)
    {
        var types = DictionaryExtensions.GetOrAdd(module.Properties, ServiceBusConsumerTypesKey, () => new HashSet<Type>());
        types.Add(type);
    }

    /// <summary>
    /// Returns all collected types for discovery of service bus consumers.
    /// </summary>
    private static IEnumerable<Type> GetServiceBusConsumerTypesFromModule(this IModule module) => DictionaryExtensions.GetOrAdd(module.Properties, ServiceBusConsumerTypesKey, () => new HashSet<Type>());
    
    /// <summary>
    /// Adds MassTransit to the service container and registers all collected assemblies for discovery of consumers.
    /// </summary>
    public static IModule AddMassTransitFromModule(this IModule module, Action<IBusRegistrationConfigurator> config)
    {
        var consumerTypes = module.GetServiceBusConsumerTypesFromModule().ToArray();

        module.Services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            bus.AddConsumers(consumerTypes);

            config?.Invoke(bus);
        });

        return module;
    }
}