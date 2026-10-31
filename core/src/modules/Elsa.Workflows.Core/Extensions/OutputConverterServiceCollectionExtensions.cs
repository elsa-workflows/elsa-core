using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Extensions;

/// <summary>
/// Adds output converter registrations to a service collection.
/// </summary>
public static class OutputConverterServiceCollectionExtensions
{
    /// <summary>
    /// Registers a discoverable output converter using its stable Converter ID as the keyed-service key.
    /// </summary>
    public static IServiceCollection AddOutputConverter<TConverter>(
        this IServiceCollection services,
        OutputConverterDescriptor descriptor,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TConverter : class, IOutputConverter
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(descriptor);

        OutputConverterRegistry.ValidateDescriptor(descriptor);
        ValidateUniqueId(services, descriptor.Id);

        var registration = new OutputConverterRegistration(descriptor, descriptor.Id, serviceLifetime);
        services.AddSingleton(registration);
        services.Add(ServiceDescriptor.DescribeKeyed(
            typeof(IOutputConverter),
            descriptor.Id,
            typeof(TConverter),
            serviceLifetime));
        services.TryAddSingleton<IOutputConverterRegistry, OutputConverterRegistry>();
        return services;
    }

    private static void ValidateUniqueId(IServiceCollection services, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Output converter IDs cannot be empty.", nameof(id));

        var existingRegistration = services
            .Where(x => x.ServiceType == typeof(OutputConverterRegistration))
            .Select(x => x.ImplementationInstance)
            .OfType<OutputConverterRegistration>()
            .FirstOrDefault(x => string.Equals(x.Descriptor.Id, id, StringComparison.OrdinalIgnoreCase));

        if (existingRegistration != null)
            throw new InvalidOperationException($"Output converter ID '{id}' is already registered or differs from '{existingRegistration.Descriptor.Id}' only by case.");
    }
}
