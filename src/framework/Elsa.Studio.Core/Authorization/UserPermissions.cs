namespace Elsa.Studio.Authorization;

/// <summary>
/// The permissions the current user holds, used only to tailor what Studio renders. The backend re-evaluates
/// every request, so this is never an authorization decision.
/// </summary>
public sealed class UserPermissions
{
    private UserPermissions(bool isKnown, IReadOnlyCollection<Permission> grants)
    {
        IsKnown = isKnown;
        Grants = grants;
    }

    /// <summary>
    /// No permission information is available (for example, an authentication provider whose tokens carry no
    /// <c>permissions</c> claims). Every check passes, so Studio behaves as it did before permission gating.
    /// </summary>
    public static UserPermissions Unknown { get; } = new(false, []);

    /// <summary>Creates a known permission set from the user's grants.</summary>
    public static UserPermissions FromGrants(IEnumerable<Permission> grants) => new(true, grants.Distinct().ToArray());

    /// <summary>Whether permission information is available. When it is not, every check passes.</summary>
    public bool IsKnown { get; }

    /// <summary>The grants held by the user. Empty when <see cref="IsKnown"/> is <c>false</c>.</summary>
    public IReadOnlyCollection<Permission> Grants { get; }

    /// <summary>Whether the user holds a grant satisfying <paramref name="required"/>.</summary>
    public bool Has(Permission required) => !IsKnown || PermissionMatcher.Satisfies(Grants, required);

    /// <summary>Whether the user holds a grant satisfying <c>{resource}:{verb}</c>.</summary>
    public bool Has(string resource, string verb) => Has(new Permission(resource, verb));

    /// <summary>Whether the user holds grants satisfying every one of <paramref name="required"/>.</summary>
    public bool HasAll(IEnumerable<Permission> required) => required.All(Has);

    /// <summary>Whether the user holds a grant satisfying at least one of <paramref name="required"/>.</summary>
    public bool HasAny(IEnumerable<Permission> required) => required.Any(Has);

    /// <summary>The permissions in <paramref name="required"/> the user does not hold.</summary>
    public IReadOnlyCollection<Permission> GetMissing(IEnumerable<Permission> required) => required.Where(x => !Has(x)).ToArray();

    /// <summary>
    /// Whether <paramref name="other"/> describes the same permissions: both unknown, or both known with the same grants.
    /// A renewed sign-in resolves a new instance even when nothing changed, so compare with this rather than by reference.
    /// </summary>
    public bool IsEquivalentTo(UserPermissions? other) =>
        other != null && IsKnown == other.IsKnown && Grants.Count == other.Grants.Count && Grants.All(other.Grants.Contains);
}
