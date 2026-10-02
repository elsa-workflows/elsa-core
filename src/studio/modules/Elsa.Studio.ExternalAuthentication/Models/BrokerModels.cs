using Elsa.Studio.Authentication.Abstractions.Models;

namespace Elsa.Studio.ExternalAuthentication.Models;

public sealed record LoginMethodsResponse(IReadOnlyCollection<LoginMethodDescriptor> Methods, string? PreferredMethodKey);

/// <summary>All fields required by an Elsa broker authorization request.</summary>
public sealed record BrokerAuthorizationRequest(
    string ClientId,
    string RedirectUri,
    string ResponseType,
    string CodeChallenge,
    string CodeChallengeMethod,
    string ReturnPath,
    string? State = null);

public sealed record LocalBrokerAuthorizationRequest(
    string ClientId,
    string RedirectUri,
    string ResponseType,
    string CodeChallenge,
    string CodeChallengeMethod,
    string ReturnPath,
    string Username,
    string Password,
    string? State = null);

public sealed record LocalBrokerAuthorizationResponse(string RedirectUri);

public sealed record BrokerTokenRequest(
    string GrantType,
    string ClientId,
    string? RedirectUri = null,
    string? Code = null,
    string? CodeVerifier = null,
    string? RefreshToken = null);

/// <summary>Tokens returned by the Elsa broker. Host implementations decide where these are held.</summary>
public sealed record BrokerTokenResponse(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    string RefreshToken,
    long RefreshExpiresIn,
    long ExternalSessionExpiresIn);

public sealed record BrokerLogoutRequest(string ClientId, string PostLogoutRedirectUri, string Mode);
public sealed record BrokerLogoutResponse(bool Completed, string? NavigationUrl, string? RedirectUri);

/// <summary>Safe broker error returned to the client callback or token endpoint.</summary>
public sealed record BrokerError(string Error, string Message, string CorrelationId);

/// <summary>Immutable browser routes registered for Elsa Studio broker clients.</summary>
public static class ExternalAuthenticationCallbackPaths
{
    public const string SignIn = "/authentication/external/callback";
    public const string Logout = "/authentication/external/logout-callback";
}

/// <summary>Deployment-owned registration of this Studio host with the Elsa broker.</summary>
public sealed class ExternalAuthenticationClientOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = ExternalAuthenticationCallbackPaths.SignIn;
    public string LogoutCallbackPath { get; set; } = ExternalAuthenticationCallbackPaths.Logout;
    public string? ClientSecret { get; set; }
    public string? SecurityWarning { get; set; }
}

/// <summary>Represents the host-owned authenticated broker session without exposing a refresh token to UI code.</summary>
public interface IExternalAuthenticationTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
