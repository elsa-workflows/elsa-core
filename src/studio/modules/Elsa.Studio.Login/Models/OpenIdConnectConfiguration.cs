namespace Elsa.Studio.Login.Models;

/// <summary>
/// Provides configuration necessary for correct OpenIdConnect operation
/// </summary>
public class OpenIdConnectConfiguration
{
    /// <summary>
    /// The authorization_endpoint as given by the openid-configuration
    /// </summary>
    public required string AuthEndpoint { get; set; }

    /// <summary>
    /// The token_endpoint as given by the openid-configuration
    /// </summary>
    public required string TokenEndpoint { get; set; }

    /// <summary>
    /// The end_session_endpoint as given by the openid-configuration
    /// </summary>
    public required string EndSessionEndpoint { get; set; }

    /// <summary>
    /// The client_id as which this application is registered with the authorization server
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// The client_secret as which this application is registered with the authorization server
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// The scopes to request, defaulting to: openid profile offline_access
    /// </summary>
    public string[] Scopes { get; set; } = ["openid", "profile", "offline_access"];

    /// <summary>
    /// Enables PKCE (Proof Key for Code Exchange) for the authorization code flow.
    /// </summary>
    public bool UsePkce { get; set; } = false;
}
