namespace Elsa.ModularPersistence.Validation;

public sealed record StorageProviderCapabilitiesRegistration(string ProviderName, ProviderCapabilities Capabilities)
{
    public string ProviderName { get; } = string.IsNullOrWhiteSpace(ProviderName) ? throw new ArgumentException("Provider name is required.", nameof(ProviderName)) : ProviderName;

    public ProviderCapabilities Capabilities { get; } = Capabilities ?? throw new ArgumentNullException(nameof(Capabilities));
}
