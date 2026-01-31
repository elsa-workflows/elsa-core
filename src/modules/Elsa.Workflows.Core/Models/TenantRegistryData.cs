using System.Collections.Concurrent;

namespace Elsa.Workflows.Models;

/// <summary>
/// Encapsulates the activity descriptor dictionaries for a single tenant or for tenant-agnostic activities.
/// </summary>
public class TenantRegistryData
{
    /// <summary>
    /// Maps (ActivityType, Version) to ActivityDescriptor.
    /// </summary>
    public ConcurrentDictionary<(string Type, int Version), ActivityDescriptor> ActivityDescriptors { get; } = new();

    /// <summary>
    /// Maps provider type to the collection of descriptors provided by that provider.
    /// </summary>
    public ConcurrentDictionary<Type, ICollection<ActivityDescriptor>> ProvidedActivityDescriptors { get; } = new();
}
