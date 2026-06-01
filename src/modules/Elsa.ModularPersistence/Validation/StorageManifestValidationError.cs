namespace Elsa.ModularPersistence.Validation;

/// <summary>
/// Describes a manifest validation failure.
/// </summary>
public sealed record StorageManifestValidationError(string Code, string Message, string Path);
