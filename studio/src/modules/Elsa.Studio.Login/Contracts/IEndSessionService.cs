namespace Elsa.Studio.Login.Contracts;

/// <summary>
/// Provides a service to end the session with an authorization server
/// </summary>
public interface IEndSessionService
{
    /// <summary>
    /// Remove authentication state and end the session with the authorization server
    /// </summary>
    Task LogoutAsync();
}