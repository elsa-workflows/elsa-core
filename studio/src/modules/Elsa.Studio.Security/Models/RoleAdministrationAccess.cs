namespace Elsa.Studio.Security.Models;

public enum RoleAdministrationAccessState
{
    Ready,
    Forbidden,
    Unavailable
}

/// <summary>
/// Independent capabilities for viewing and mutating roles.
/// </summary>
public sealed record RoleAdministrationAccess(
    RoleAdministrationAccessState State,
    bool CanView,
    bool CanCreate,
    bool CanUpdate,
    bool CanDelete)
{
    public static RoleAdministrationAccess Forbidden { get; } =
        new(RoleAdministrationAccessState.Forbidden, false, false, false, false);

    public static RoleAdministrationAccess Unavailable { get; } =
        new(RoleAdministrationAccessState.Unavailable, false, false, false, false);
}
