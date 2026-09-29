namespace Elsa.Studio.Authentication.ElsaIdentity.Contracts;

/// <summary>
/// Ends the current ElsaIdentity session.
/// </summary>
public interface ISignOutService
{
    /// <summary>
    /// Clears the stored tokens, publishes the anonymous authentication state and navigates to the login page.
    /// </summary>
    Task SignOutAsync();
}
