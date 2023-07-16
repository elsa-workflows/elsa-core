namespace Elsa.Api.Client.Resources.StorageDrivers.Models;

/// <summary>
/// Represents a storage driver descriptor.
/// </summary>
/// <param name="TypeName">The type name of the storage driver.</param>
/// <param name="DisplayName">The display name of the storage driver.</param>
public record StorageDriverDescriptor(string TypeName, string DisplayName);