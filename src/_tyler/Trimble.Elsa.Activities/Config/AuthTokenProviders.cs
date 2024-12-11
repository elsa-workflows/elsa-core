namespace Trimble.Elsa.Activities.Config;

/// <summary>
/// Configuration values required for obtaining tokens from providers. 
/// These tokens are needed for making HTTP requests during provisioning
/// execution of a <see cref="HttpRequestProvisionAction"/>.
/// </summary>
public record AuthnTokenProviders
{
    /// <summary>
    /// The SalesforceDX token provider authn configuration values.
    /// </summary>
    public required SalesforceDXTokenProvider SalesforceDX { get; set; }

    /// <summary>
    /// The TrimbleId token provider authn configuration values.
    /// </summary>
    public required TrimbleIdTokenProvider TrimbleId { get; set; }

    /// <summary>
    /// The ViewpointId token provider authn configuration values.
    /// </summary>
    public required ViewpointIdTokenProvider ViewpointId { get; set; }
}