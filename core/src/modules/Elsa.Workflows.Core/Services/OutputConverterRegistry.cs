using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <inheritdoc />
public class OutputConverterRegistry : IOutputConverterRegistry
{
    private readonly IReadOnlyDictionary<string, OutputConverterRegistration> _registrations;

    public OutputConverterRegistry(IEnumerable<OutputConverterRegistration> registrations)
    {
        var registrationList = registrations.ToList();
        ValidateRegistrations(registrationList);
        _registrations = registrationList.ToDictionary(x => x.Descriptor.Id, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public IEnumerable<OutputConverterDescriptor> ListAll() => _registrations.Values.Select(x => x.Descriptor);

    /// <inheritdoc />
    public OutputConverterDescriptor? Find(string id) =>
        _registrations.TryGetValue(id, out var registration) ? registration.Descriptor : null;

    /// <summary>
    /// Finds the internal keyed-service registration associated with the specified converter ID.
    /// </summary>
    public OutputConverterRegistration? FindRegistration(string id) =>
        _registrations.TryGetValue(id, out var registration) ? registration : null;

    /// <inheritdoc />
    public IEnumerable<OutputConverterDescriptor> FindCompatible(Type sourceType, Type destinationType) =>
        ListAll().Where(x =>
            x.SourceType.IsAssignableFrom(sourceType) &&
            IsAssignableToDestination(x.ResultType, destinationType));

    internal static bool IsAssignableToDestination(Type valueType, Type destinationType)
    {
        if (destinationType.IsAssignableFrom(valueType))
            return true;

        var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
        return underlyingDestinationType?.IsAssignableFrom(valueType) == true;
    }

    private static void ValidateRegistrations(IReadOnlyCollection<OutputConverterRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            ValidateDescriptor(registration.Descriptor);
            var id = registration.Descriptor.Id;

            if (!string.Equals(id, registration.ServiceKey, StringComparison.Ordinal))
                throw new InvalidOperationException($"Output converter registration '{id}' must use the Converter ID as its service key.");
        }

        var duplicate = registrations
            .GroupBy(x => x.Descriptor.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicate != null)
            throw new InvalidOperationException($"Output converter ID '{duplicate.Key}' is registered more than once or differs from another registration only by case.");
    }

    internal static void ValidateDescriptor(OutputConverterDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Id))
            throw new InvalidOperationException("Output converter IDs cannot be empty.");

        if (descriptor.SourceType.ContainsGenericParameters || descriptor.ResultType.ContainsGenericParameters)
            throw new InvalidOperationException($"Output converter '{descriptor.Id}' cannot use open-generic source or result types.");
    }
}
