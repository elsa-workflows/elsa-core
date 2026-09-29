namespace Elsa.Studio.Authentication.OpenIdConnect.Models;

/// <summary>
/// Configuration options for OpenID Connect authentication.
/// </summary>
public class OidcOptions
{
    /// <summary>
    /// Scopes requested during authentication/sign-in.
    /// These typically include identity-related scopes like openid, profile, email.
    /// </summary>
    public string[] AuthenticationScopes { get; set; } = ["openid", "profile", "offline_access"];

    /// <summary>
    /// Scopes requested when obtaining access tokens for backend API calls.
    /// Used in multi-audience scenarios where the API requires different scopes.
    /// </summary>
    public string[] BackendApiScopes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether to require HTTPS for metadata endpoints.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;
    /// <summary>
    /// Gets or sets the authority URL of the OpenID Connect provider.
    /// </summary>
    public string Authority { get; set; } = default!;

    /// <summary>
    /// Gets or sets the client ID registered with the OpenID Connect provider.
    /// </summary>
    public string ClientId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the client secret (optional, typically not used with public clients like WASM).
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the response type for the authentication request.
    /// </summary>
    public string ResponseType { get; set; } = "code";

    /// <summary>
    /// Gets or sets whether to use PKCE (Proof Key for Code Exchange).
    /// </summary>
    public bool UsePkce { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to save tokens in the authentication properties (Server only).
    /// </summary>
    public bool SaveTokens { get; set; } = true;

    /// <summary>
    /// Gets or sets the callback path for handling the authentication response.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Blazor Server typically uses <c>/signin-oidc</c>.</description></item>
    /// <item><description>Blazor WebAssembly uses <c>/authentication/login-callback</c>.</description></item>
    /// </list>
    /// When using the Blazor WebAssembly authentication stack, the identity provider expects an absolute <c>redirect_uri</c>.
    /// The framework will convert these paths into absolute URIs based on the current base URI.
    /// </remarks>
    public string? CallbackPath { get; set; }

    /// <summary>
    /// Gets or sets the sign-out callback path.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Blazor Server typically uses <c>/signout-callback-oidc</c>.</description></item>
    /// <item><description>Blazor WebAssembly uses <c>/authentication/logout-callback</c>.</description></item>
    /// </list>
    /// </remarks>
    public string? SignedOutCallbackPath { get; set; }

    /// <summary>
    /// Gets or sets whether to get claims from the user info endpoint.
    /// </summary>
    /// <remarks>
    /// When enabled, the OIDC handler calls the provider's <c>userinfo</c> endpoint after authentication.
    /// Some providers or app registrations may return 401 from <c>userinfo</c> unless specific permissions/scopes
    /// are configured.
    /// </remarks>
    public bool GetClaimsFromUserInfoEndpoint { get; set; } = false;

    /// <summary>
    /// Gets or sets the metadata address (optional, auto-discovered from Authority if not set).
    /// </summary>
    public string? MetadataAddress { get; set; }

    /// <summary>
    /// Gets or sets the claim type to use for the user's name.
    /// </summary>
    public string NameClaimType { get; set; } = "name";

    /// <summary>
    /// Gets or sets the claim type to use for the user's roles.
    /// </summary>
    public string RoleClaimType { get; set; } = "role";
}
