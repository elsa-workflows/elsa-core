using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Validation;

/// <summary>
/// Validates a manifest against provider capabilities and portable descriptor rules.
/// </summary>
public sealed class StorageManifestValidator
{
    public StorageManifestValidationResult Validate(StorageManifestDescriptor manifest, ProviderCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(capabilities);

        var errors = new List<StorageManifestValidationError>();

        foreach (var storageUnit in manifest.StorageUnits)
            ValidateStorageUnit(storageUnit, capabilities, errors);

        return errors.Count == 0 ? StorageManifestValidationResult.Success : new StorageManifestValidationResult(errors);
    }

    private static void ValidateStorageUnit(StorageUnitDescriptor storageUnit, ProviderCapabilities capabilities, ICollection<StorageManifestValidationError> errors)
    {
        var storageUnitPath = $"storageUnits['{storageUnit.Name}']";

        if (!capabilities.StorageUnitKinds.Contains(storageUnit.Kind))
        {
            errors.Add(new StorageManifestValidationError(
                "UnsupportedStorageUnitKind",
                $"Provider does not support storage unit kind '{storageUnit.Kind}'.",
                $"{storageUnitPath}.kind"));
        }

        if (!capabilities.PhysicalizationIntents.Contains(storageUnit.PhysicalizationIntent))
        {
            errors.Add(new StorageManifestValidationError(
                "UnsupportedPhysicalizationIntent",
                $"Provider does not support physicalization intent '{storageUnit.PhysicalizationIntent}'.",
                $"{storageUnitPath}.physicalizationIntent"));
        }

        if (storageUnit.Keys.All(x => x.Kind != StorageKeyKind.Primary))
        {
            errors.Add(new StorageManifestValidationError(
                "MissingPrimaryKey",
                $"Storage unit '{storageUnit.Name}' must declare a primary key.",
                $"{storageUnitPath}.keys"));
        }

        foreach (var field in storageUnit.Fields)
            ValidateField(field, capabilities, errors, $"{storageUnitPath}.fields['{field.Name}']");

        foreach (var index in storageUnit.Indexes)
            ValidateIndex(index, capabilities, errors, $"{storageUnitPath}.indexes['{index.Name}']");
    }

    private static void ValidateField(StorageFieldDescriptor field, ProviderCapabilities capabilities, ICollection<StorageManifestValidationError> errors, string path)
    {
        if (capabilities.FieldTypes.Contains(field.Type))
            return;

        errors.Add(new StorageManifestValidationError(
            "UnsupportedFieldType",
            $"Provider does not support field type '{field.Type}'.",
            $"{path}.type"));
    }

    private static void ValidateIndex(StorageIndexDescriptor index, ProviderCapabilities capabilities, ICollection<StorageManifestValidationError> errors, string path)
    {
        if (!capabilities.PhysicalizationIntents.Contains(index.PhysicalizationIntent))
        {
            errors.Add(new StorageManifestValidationError(
                "UnsupportedPhysicalizationIntent",
                $"Provider does not support physicalization intent '{index.PhysicalizationIntent}'.",
                $"{path}.physicalizationIntent"));
        }

        if (index.Fields.Count > capabilities.MaxIndexFieldCount)
        {
            errors.Add(new StorageManifestValidationError(
                "TooManyIndexFields",
                $"Index '{index.Name}' declares {index.Fields.Count} fields, but the provider supports at most {capabilities.MaxIndexFieldCount}.",
                $"{path}.fields"));
        }
    }
}
