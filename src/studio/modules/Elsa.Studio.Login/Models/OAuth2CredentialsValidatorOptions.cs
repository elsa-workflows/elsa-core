namespace Elsa.Studio.Login.Models;

/// <summary>
/// Represents configuration options for the OAuth2 credentials validation process.
/// </summary>
public class OAuth2CredentialsValidatorOptions
{
    /// <summary>
    /// The URL of the OAuth2 token endpoint.
    /// </summary>
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// The client ID to use for the OAuth2 client.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The client secret to use for the OAuth2 client (if confidential).
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// The scope of access being requested during the OAuth2 authentication process.
    /// Typically, this defines the permissions or resources the client is requesting
    /// authorization for.
    /// </summary>
    public string? Scope { get; set; }
}