using Elsa.Studio.ExternalAuthentication.Models;
using Refit;

namespace Elsa.Studio.ExternalAuthentication.Client;

/// <summary>Anonymous discovery endpoint. Its payload deliberately contains presentation data only.</summary>
public interface ILoginMethodsApi
{
    [Get("/external-authentication/login-methods")]
    Task<LoginMethodsResponse> ListAsync([AliasAs("clientId")] string clientId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Raw broker endpoint contract. It is intentionally resolved through the anonymous provider for code exchange
/// and never uses the regular bearer handler, which would create a sign-in/refresh recursion.
/// </summary>
public interface IExternalAuthenticationBrokerApi
{
    [Post("/external-authentication/local/authorize")]
    Task<LocalBrokerAuthorizationResponse> AuthorizeLocalAsync([Body] LocalBrokerAuthorizationRequest request, CancellationToken cancellationToken = default);

    [Post("/external-authentication/token")]
    [Headers("Content-Type: application/x-www-form-urlencoded")]
    Task<BrokerTokenResponse> ExchangeAsync([Body(BodySerializationMethod.UrlEncoded)] BrokerTokenRequest request, [Header("Authorization")] string? authorization = null, CancellationToken cancellationToken = default);

    [Post("/external-authentication/logout")]
    Task<BrokerLogoutResponse> LogoutAsync([Body] BrokerLogoutRequest request, [Header("Authorization")] string? authorization = null, CancellationToken cancellationToken = default);
}
