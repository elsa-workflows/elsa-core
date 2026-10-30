namespace Elsa.Authorization;

/// <summary>
/// The permission requirement an endpoint declares: the caller must hold a permission satisfying at least one of
/// <see cref="AnyOf"/>. An endpoint declared with <c>RequirePermission</c> has exactly one; an endpoint declared with
/// <c>RequireAnyPermission</c> may have several.
/// </summary>
public sealed class EndpointPermissionRequirement
{
    /// <summary>Creates a requirement satisfied by any one of <paramref name="anyOf"/>. Duplicates are collapsed.</summary>
    /// <exception cref="ArgumentException"><paramref name="anyOf"/> is empty, so nothing could satisfy the requirement.</exception>
    public EndpointPermissionRequirement(IEnumerable<Permission> anyOf)
    {
        AnyOf = Array.AsReadOnly(anyOf.Distinct().ToArray());

        if (AnyOf.Count == 0)
        {
            throw new ArgumentException("An endpoint permission requirement needs at least one permission; one with none could never be satisfied.", nameof(anyOf));
        }
    }

    /// <summary>The permissions that each satisfy the requirement on their own, in declaration order. Never empty.</summary>
    public IReadOnlyCollection<Permission> AnyOf { get; }
}
