namespace Elsa.ModularPersistence.Validation;

public interface IStorageProviderCapabilitiesRegistry
{
    ProviderCapabilities GetCapabilities(string providerName);
}
