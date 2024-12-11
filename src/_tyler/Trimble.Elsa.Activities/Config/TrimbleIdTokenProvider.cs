namespace Trimble.Elsa.Activities.Config;

/// <summary>
/// Configurations needed when requesting tokens from the TrimbleId token provider.
/// </summary>
public class TrimbleIdTokenProvider : ClientCredentialsGrantConfig
{
    /// Indicates whether or not TrimbleId token provider is enabled.
    /// </summary>
    public static bool Enabled { get; set; } = false;

    /// <summary>
    /// The client ID for the TrimbleId token provider.
    /// </summary>
    public override string ClientId { get; set; } = "";

    /// <summary>
    /// The client secret for the TrimbleId token provider.
    /// </summary>
    public override string ClientSecret { get; set; } = "";

    /// <summary>
    /// The scopes to request
    /// </summary>
    public override string Scopes { get; set; } = "";

    /// <summary>
    /// The URL for obtaining a token from the TrimbleId token provider.
    /// </summary>
    public override string TokenUrl { get; set; } = "";
}