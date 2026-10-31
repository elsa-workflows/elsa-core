namespace Elsa.Api.Client.Resources.StorageDrivers.Models;

/// <summary>
/// Represents a storage driver descriptor.
/// </summary>
/// <param name="TypeName">The type name of the storage driver.</param>
/// <param name="DisplayName">The display name of the storage driver.</param>
/// <param name="Priority">The priority of the storage driver.</param>
/// <param name="Deprecated">Indicates whether the storage driver is deprecated.</param>
public record StorageDriverDescriptor(string TypeName, string DisplayName, double Priority = 0, bool Deprecated = false);