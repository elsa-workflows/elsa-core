using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Validation;

/// <summary>
/// Describes the portable manifest features a provider supports.
/// </summary>
public sealed record ProviderCapabilities
{
    public ProviderCapabilities(
        IEnumerable<StorageUnitKind> storageUnitKinds,
        IEnumerable<StorageFieldType> fieldTypes,
        IEnumerable<PhysicalizationIntent> physicalizationIntents,
        int maxIndexFieldCount = 16)
    {
        if (maxIndexFieldCount < 1)
            throw new ArgumentOutOfRangeException(nameof(maxIndexFieldCount), "Maximum index field count must be greater than zero.");

        StorageUnitKinds = RequireCapabilityValues(storageUnitKinds, nameof(storageUnitKinds));
        FieldTypes = RequireCapabilityValues(fieldTypes, nameof(fieldTypes));
        PhysicalizationIntents = RequireCapabilityValues(physicalizationIntents, nameof(physicalizationIntents));
        MaxIndexFieldCount = maxIndexFieldCount;
    }

    public IReadOnlySet<StorageUnitKind> StorageUnitKinds { get; }

    public IReadOnlySet<StorageFieldType> FieldTypes { get; }

    public IReadOnlySet<PhysicalizationIntent> PhysicalizationIntents { get; }

    public int MaxIndexFieldCount { get; }

    public static ProviderCapabilities PortableDocument { get; } = new(
        [StorageUnitKind.Document],
        Enum.GetValues<StorageFieldType>(),
        [PhysicalizationIntent.PortableDocument]);

    private static IReadOnlySet<TEnum> RequireCapabilityValues<TEnum>(IEnumerable<TEnum>? values, string parameterName) where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);

        var result = values.ToHashSet();
        if (result.Count == 0)
            throw new ArgumentException("At least one capability value is required.", parameterName);

        foreach (var value in result)
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(parameterName, value, $"Unknown {typeof(TEnum).Name} capability value.");
        }

        return result;
    }
}
