namespace Elsa.Studio.Authentication.OpenIdConnect.Contracts;

/// <summary>
/// Provides backend API scopes for multi-audience authentication scenarios.
/// </summary>
/// <remarks>
/// This interface allows authentication providers (e.g., OIDC) to specify different scopes
/// for backend API calls versus sign-in authentication.
/// </remarks>
public interface IBackendApiScopeProvider
{
    /// <summary>
    /// Gets the scopes to request when obtaining access tokens for backend API calls.
    /// </summary>
    /// <returns>An array of scope strings, or an empty array if no backend API scopes are configured.</returns>
    string[] GetBackendApiScopes();
}
