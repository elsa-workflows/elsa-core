using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Planning;

public interface IStoragePhysicalizationPlanner
{
    string ProviderName { get; }

    StoragePhysicalizationPlan Plan(StorageManifestDescriptor manifest);
}
