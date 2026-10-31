namespace Elsa.Studio.Authorization;

/// <summary>
/// Declares a permission a routable page requires. Studio's shell renders an access-denied state instead of the
/// page, so the page never issues API calls the backend would reject. Multiple attributes are all required.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string resource, string verb) : Attribute
{
    /// <summary>The required permission.</summary>
    public Permission Permission { get; } = new(resource, verb);

    /// <summary>Returns every permission declared on <paramref name="type"/>, including inherited declarations.</summary>
    public static IReadOnlyCollection<Permission> GetRequiredPermissions(Type type) =>
        type.GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true)
            .Cast<RequirePermissionAttribute>()
            .Select(x => x.Permission)
            .ToArray();
}
