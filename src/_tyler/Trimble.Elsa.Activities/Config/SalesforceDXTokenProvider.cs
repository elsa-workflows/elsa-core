namespace Trimble.Elsa.Activities.Config;

/// <summary>
/// Configurations needed when requesting tokens from the SalesforceDX token provider.
/// </summary>
public class SalesforceDXTokenProvider : PasswordGrantConfig
{
    /// <summary>
    /// The unique configuration key for this configuration object.
    /// </summary>
    public static string SalesforceTokenProviderKey => "SalesforceDXTokenProvider";

    /// <summary>
    /// Indicates whether or not SalesforceDX token provider is enabled.
    /// </summary>
    public static bool Enabled { get; set; } = false;

    /// <summary>
    /// The client ID for the SalesforceDX token provider.
    /// </summary>
    public override string ClientId { get; set; } = "";

    /// <summary>
    /// The client secret for the SalesforceDX token provider.
    /// </summary>
    public override string ClientSecret { get; set; } = "";

    /// <summary>
    /// The username for the SalesforceDX token provider.
    /// </summary>
    public override string Username { get; set; } = "";

    /// <summary>
    /// The user password for the SalesforceDX token provider.
    /// </summary>
    public override string Password { get; set; } = "";

    /// <summary>
    /// The user token for the SalesforceDX token provider.
    /// </summary>
    public override string UserToken { get; set; } = "";

    /// <summary>
    /// The URL for obtaining a token from the SalesforceDX token provider.
    /// </summary>
    public override string TokenUrl { get; set; } = "";
}