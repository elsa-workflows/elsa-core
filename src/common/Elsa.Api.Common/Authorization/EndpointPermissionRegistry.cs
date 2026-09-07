using System.Collections.Concurrent;

namespace Elsa.Authorization;

/// <summary>
/// Records the permission each endpoint declares, keyed by endpoint type.
/// </summary>
/// <remarks>
/// The requirement itself is attached as an inline authorization policy, which is not readable back from
/// the endpoint definition. Recording it here keeps the declaration introspectable: it is what lets a
/// deployment answer "what does this endpoint require" without reading source, and it gives tests a way to
/// assert a specific requirement rather than merely that one exists.
/// </remarks>
public static class EndpointPermissionRegistry
{
    private static readonly ConcurrentDictionary<Type, Permission> Declared = new();

    /// <summary>Records that <paramref name="endpointType"/> requires <paramref name="permission"/>.</summary>
    public static void Record(Type endpointType, Permission permission) => Declared[endpointType] = permission;

    /// <summary>The permission <paramref name="endpointType"/> declares, if it declares one.</summary>
    public static Permission? Find(Type endpointType) => Declared.TryGetValue(endpointType, out var permission) ? permission : null;

    /// <summary>Every recorded declaration.</summary>
    public static IReadOnlyDictionary<Type, Permission> All => Declared;
}
