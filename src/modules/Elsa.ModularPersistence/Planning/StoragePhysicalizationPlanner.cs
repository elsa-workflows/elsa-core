using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Validation;

namespace Elsa.ModularPersistence.Planning;

public sealed class StoragePhysicalizationPlanner(string providerName, ProviderCapabilities capabilities) : IStoragePhysicalizationPlanner
{
    public string ProviderName { get; } = string.IsNullOrWhiteSpace(providerName) ? throw new ArgumentException("Provider name is required.", nameof(providerName)) : providerName;

    public StoragePhysicalizationPlan Plan(StorageManifestDescriptor manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var items = new List<PhysicalizationPlanItem>();
        foreach (var storageUnit in manifest.StorageUnits)
        {
            items.Add(CreateItem(
                PhysicalizationPlanItemKind.StorageUnit,
                $"storageUnits['{storageUnit.Name}']",
                storageUnit.Name,
                storageUnit.PhysicalizationIntent,
                capabilities.PhysicalizationIntents.Contains(storageUnit.PhysicalizationIntent),
                DescribeStorageUnit(storageUnit.PhysicalizationIntent)));

            foreach (var index in storageUnit.Indexes)
            {
                items.Add(CreateItem(
                    PhysicalizationPlanItemKind.Index,
                    $"storageUnits['{storageUnit.Name}'].indexes['{index.Name}']",
                    index.Name,
                    index.PhysicalizationIntent,
                    capabilities.PhysicalizationIntents.Contains(index.PhysicalizationIntent),
                    DescribeIndex(index.PhysicalizationIntent)));
            }
        }

        return new StoragePhysicalizationPlan(ProviderName, manifest.SchemaName, manifest.Version.ToString(), items);
    }

    private static PhysicalizationPlanItem CreateItem(PhysicalizationPlanItemKind kind, string path, string name, PhysicalizationIntent intent, bool supported, string supportedMessage)
    {
        var status = supported ? PhysicalizationPlanStatus.Planned : PhysicalizationPlanStatus.Unsupported;
        var message = supported ? supportedMessage : $"Provider does not support physicalization intent '{intent}'.";
        return new PhysicalizationPlanItem(kind, path, name, intent, status, message);
    }

    private static string DescribeStorageUnit(PhysicalizationIntent intent) =>
        intent switch
        {
            PhysicalizationIntent.PortableDocument => "Store documents using the provider-neutral portable document shape.",
            PhysicalizationIntent.OptimizedIndexes => "Store documents portably and allow provider-optimized index structures.",
            PhysicalizationIntent.NativePhysicalized => "Materialize the storage unit using provider-native structures.",
            _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, "Unknown physicalization intent.")
        };

    private static string DescribeIndex(PhysicalizationIntent intent) =>
        intent switch
        {
            PhysicalizationIntent.PortableDocument => "Use the portable document index representation.",
            PhysicalizationIntent.OptimizedIndexes => "Create provider-specific optimized index structures.",
            PhysicalizationIntent.NativePhysicalized => "Materialize the index using provider-native structures.",
            _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, "Unknown physicalization intent.")
        };
}
