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

    /// <summary>Whether the user holds grants satisfying every one of <paramref name="required"/>.</summary>
    public bool HasAll(IEnumerable<Permission> required) => required.All(Has);

    /// <summary>The permissions in <paramref name="required"/> the user does not hold.</summary>
    public IReadOnlyCollection<Permission> GetMissing(IEnumerable<Permission> required) => required.Where(x => !Has(x)).ToArray();
}
