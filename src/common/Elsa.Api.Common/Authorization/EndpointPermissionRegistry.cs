using System.Collections.Concurrent;

namespace Elsa.Authorization;

/// <summary>
/// Records the permission requirement each endpoint declares, keyed by endpoint type.
/// </summary>
/// <remarks>
/// The requirement itself is attached as an inline authorization policy, which is not readable back from
/// the endpoint definition. Recording it here keeps the declaration introspectable: it is what lets a
/// deployment answer "what does this endpoint require" without reading source, and it gives tests a way to
/// assert a specific requirement rather than merely that one exists.
/// <para>
/// A requirement is a set of permissions any one of which satisfies it. <see cref="FindRequirement"/> and
/// <see cref="AllRequirements"/> report every declaration. <see cref="Find"/> and <see cref="All"/> predate
/// "any of" requirements and report only those that consist of exactly one permission, so they report nothing for
/// an endpoint accepting any of several.
/// </para>
/// </remarks>
public static class EndpointPermissionRegistry
{
    private static readonly ConcurrentDictionary<Type, EndpointPermissionRequirement> Requirements = new();
    private static readonly ConcurrentDictionary<Type, Permission> SinglePermissions = new();
    private static readonly object WriteLock = new();

    /// <summary>Records that <paramref name="endpointType"/> requires <paramref name="permission"/>.</summary>
    public static void Record(Type endpointType, Permission permission) => Record(endpointType, new EndpointPermissionRequirement([permission]));

    /// <summary>Records that <paramref name="endpointType"/> requires <paramref name="requirement"/>, replacing what it recorded before.</summary>
    public static void Record(Type endpointType, EndpointPermissionRequirement requirement)
    {
        // Writers are serialized so the two views cannot disagree about a type recorded twice concurrently.
        lock (WriteLock)
        {
            Requirements[endpointType] = requirement;

            if (requirement.AnyOf.Count == 1)
            {
                SinglePermissions[endpointType] = requirement.AnyOf.Single();
            }
            else
            {
                SinglePermissions.TryRemove(endpointType, out _);
            }
        }
    }

    /// <summary>
    /// The permission <paramref name="endpointType"/> declares, if it requires exactly one. <c>null</c> both when it
    /// declares none and when it accepts any of several; <see cref="FindRequirement"/> tells the two apart.
    /// </summary>
    public static Permission? Find(Type endpointType) => SinglePermissions.TryGetValue(endpointType, out var permission) ? permission : null;

    /// <summary>The requirement <paramref name="endpointType"/> declares, if it declares one.</summary>
    public static EndpointPermissionRequirement? FindRequirement(Type endpointType) => Requirements.TryGetValue(endpointType, out var requirement) ? requirement : null;

    /// <summary>Every recorded declaration of exactly one permission. Omits endpoints accepting any of several; see <see cref="AllRequirements"/>.</summary>
    public static IReadOnlyDictionary<Type, Permission> All => SinglePermissions;

    /// <summary>Every recorded declaration.</summary>
    public static IReadOnlyDictionary<Type, EndpointPermissionRequirement> AllRequirements => Requirements;
}
