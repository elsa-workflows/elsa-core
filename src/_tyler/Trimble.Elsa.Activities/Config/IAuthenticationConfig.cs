namespace Trimble.Elsa.Activities.Config;

/// <summary>
/// Determines the OUATH2 grant type to use when requesting a token.
/// </summary>
public enum GrantType
{
    client_credentials,
    password
}

/// <summary>
/// The shared base class for all OAUTH2 grant types
/// </summary>
public abstract class TokenRequestConfig
{
    public abstract string TokenUrl { get; set; }

    public abstract string ClientId { get; set; }

    public abstract string ClientSecret { get; set; }

    public abstract GrantType GrantType { get; }
}

/// <summary>
/// Contains configuration data for a client credentials grant request
/// </summary>
public abstract class ClientCredentialsGrantConfig : TokenRequestConfig
{
    public abstract string Scopes { get; set; }

    public override GrantType GrantType => GrantType.client_credentials;
}

/// <summary>
/// Contains configuration data for a password grant request
/// </summary>
public abstract class PasswordGrantConfig : TokenRequestConfig
{
    public abstract string Username { get; set; }

    public abstract string Password { get; set; }

    public abstract string UserToken { get; set; }

    public override GrantType GrantType => GrantType.password;
}