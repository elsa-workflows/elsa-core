namespace Elsa.ModularPersistence.Validation;

public sealed class StorageProviderCapabilitiesRegistry(IEnumerable<StorageProviderCapabilitiesRegistration> registrations) : IStorageProviderCapabilitiesRegistry
{
    private readonly IReadOnlyDictionary<string, ProviderCapabilities> _registrations = registrations
        .GroupBy(x => x.ProviderName, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(x => x.Key, x => x.Last().Capabilities, StringComparer.OrdinalIgnoreCase);

    public ProviderCapabilities GetCapabilities(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        return _registrations.GetValueOrDefault(providerName) ?? ProviderCapabilities.PortableDocument;
    }
}
