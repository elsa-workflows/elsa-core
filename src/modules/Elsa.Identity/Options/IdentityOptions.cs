namespace Elsa.Identity.Options;

/// <summary>
/// Represents the identity options.
/// </summary>
public class IdentityOptions
{

    /// <summary>
    /// The issuer to use when creating and validating tokens
    /// </summary>
    public string Issuer { get; set; } = "http://elsa.api";

    /// <summary>
    /// The audience to use when creating and validating tokens
    /// </summary>
    public string Audience { get; set; } = "http://elsa.api";
}