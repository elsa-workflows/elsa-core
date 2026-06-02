namespace Elsa.ModularPersistence.Validation;

/// <summary>
/// Contains manifest validation errors.
/// </summary>
public sealed record StorageManifestValidationResult
{
    public StorageManifestValidationResult(IEnumerable<StorageManifestValidationError> errors)
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyCollection<StorageManifestValidationError> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public static StorageManifestValidationResult Success { get; } = new([]);
}
