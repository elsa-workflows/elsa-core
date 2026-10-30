namespace Elsa.Studio.Authentication.ElsaIdentity.Contracts;

/// <summary>
/// Ends the current ElsaIdentity session.
/// </summary>
public interface ISignOutService
{
    /// <summary>
    /// Revokes the session at the backend when possible, clears the stored tokens, publishes the anonymous authentication state and navigates to the login page.
    /// </summary>
    Task SignOutAsync();
}
