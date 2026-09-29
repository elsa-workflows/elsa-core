namespace Elsa.Studio.Security.Models;

public enum UserAdministrationAccessState
{
    Ready,
    Forbidden,
    Unavailable
}

/// <summary>
/// Independent capabilities for viewing and mutating users.
/// </summary>
public sealed record UserAdministrationAccess(
    UserAdministrationAccessState State,
    bool CanView,
    bool CanCreate,
    bool CanUpdate,
    bool CanDelete)
{
    public static UserAdministrationAccess Forbidden { get; } =
        new(UserAdministrationAccessState.Forbidden, false, false, false, false);

    public static UserAdministrationAccess Unavailable { get; } =
        new(UserAdministrationAccessState.Unavailable, false, false, false, false);
}
