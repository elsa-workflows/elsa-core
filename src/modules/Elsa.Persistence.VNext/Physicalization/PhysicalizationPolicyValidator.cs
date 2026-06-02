using Elsa.Persistence.VNext;

namespace Elsa.Persistence.VNext.Physicalization;

public static class PhysicalizationPolicyValidator
{
    public static PersistenceStorageUnit GetStorageUnit(PersistenceSchema schema, StoragePhysicalizationPolicy policy)
    {
        var storageUnit = schema.StorageUnits.SingleOrDefault(x => x.Name == policy.StorageUnit);

        if (storageUnit == null)
            throw new InvalidOperationException($"Storage unit '{policy.StorageUnit}' was not declared by schema '{schema.Name}'.");

        var fields = storageUnit.Fields.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var index in policy.Indexes)
        {
            var missingField = index.Fields.FirstOrDefault(field => !fields.Contains(field));

            if (missingField != null)
                throw new InvalidOperationException($"Physicalized index '{index.Name}' references undeclared field '{missingField}' on storage unit '{storageUnit.Name}'.");
        }

        return storageUnit;
    }
}
