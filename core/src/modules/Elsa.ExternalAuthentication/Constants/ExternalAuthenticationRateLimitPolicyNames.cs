namespace Elsa.ExternalAuthentication.Constants;

public static class ExternalAuthenticationRateLimitPolicyNames
{
    public const string Discovery = "elsa-external-authentication-discovery";
    public const string ExternalInitiation = "elsa-external-authentication-external-initiation";
    public const string LocalInitiation = "elsa-external-authentication-local-initiation";
    public const string ProviderCallback = "elsa-external-authentication-provider-callback";
    public const string TokenExchange = "elsa-external-authentication-token-exchange";
}
