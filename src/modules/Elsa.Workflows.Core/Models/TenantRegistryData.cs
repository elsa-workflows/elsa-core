using System.Collections.Concurrent;

namespace Elsa.Workflows.Models;

/// <summary>
/// Holds the per-tenant activity descriptor dictionaries that back the activity registry.
/// </summary>
/// <remarks>
/// <para>
/// This model represents the value stored in the tenant-level registry dictionary described in ADR-0009's
/// three-dictionary architecture. The outermost dictionary typically maps a tenant identifier (or a value
/// representing tenant-agnostic scope) to an instance of <see cref="TenantRegistryData"/>. Within this class,
/// the <see cref="ActivityDescriptors"/> and <see cref="ProvidedActivityDescriptors"/> properties form the
/// inner dictionaries that index activity descriptors for that specific tenant or for tenant-agnostic activities.
/// </para>
/// <para>
/// By encapsulating these dictionaries, the registry can manage activity descriptors per tenant while maintaining
/// a consistent lookup and invalidation strategy across the entire system.
/// </para>
/// </remarks>
public class TenantRegistryData
{
    /// <summary>
    /// Primary index of activity descriptors for this tenant (or for tenant-agnostic scope).
    /// </summary>
    /// <remarks>
    /// The key is a composite of the activity <c>Type</c> (a logical activity type identifier) and its
    /// <c>Version</c>. This allows efficient lookup of a specific activity descriptor by type and version,
    /// which is the most common access pattern when compiling or executing workflows.
    /// </remarks>
    public ConcurrentDictionary<(string Type, int Version), ActivityDescriptor> ActivityDescriptors { get; } = new();

    /// <summary>
    /// Secondary index of activity descriptors grouped by their provider type.
    /// </summary>
    /// <remarks>
    /// This dictionary maps a provider <see cref="Type"/> (for example, an activity provider implementation)
    /// to the collection of <see cref="ActivityDescriptor"/> instances contributed by that provider for this
    /// tenant. It complements <see cref="ActivityDescriptors"/> by enabling provider-centric operations such as
    /// refreshing, removing, or re-registering all descriptors originating from a given provider.
    /// </remarks>
    public ConcurrentDictionary<Type, ICollection<ActivityDescriptor>> ProvidedActivityDescriptors { get; } = new();
}
