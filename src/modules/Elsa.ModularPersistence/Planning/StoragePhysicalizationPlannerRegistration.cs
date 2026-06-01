using Elsa.ModularPersistence.Validation;

namespace Elsa.ModularPersistence.Planning;

public sealed class StoragePhysicalizationPlannerRegistration(string providerName, ProviderCapabilities capabilities) : IStoragePhysicalizationPlanner
{
    private readonly StoragePhysicalizationPlanner _planner = new(providerName, capabilities);

    public string ProviderName => _planner.ProviderName;

    public StoragePhysicalizationPlan Plan(Descriptors.StorageManifestDescriptor manifest) => _planner.Plan(manifest);
}
