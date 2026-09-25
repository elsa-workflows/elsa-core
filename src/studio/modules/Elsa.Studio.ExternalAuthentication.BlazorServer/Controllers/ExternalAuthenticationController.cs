using System.Security.Cryptography;
using System.Text;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.BlazorServer.Services;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.ExternalAuthentication.BlazorServer.Controllers;

/// <summary>Confidential Studio Server broker callbacks. Completion codes and refresh tokens never enter browser code.</summary>
[Route("authentication/external")]
public sealed class ExternalAuthenticationController(
    IAnonymousBackendApiClientProvider anonymousBackendApiClientProvider,
    IServerExternalAuthenticationTransactionStore transactionStore,
    ServerExternalAuthenticationStateProvider authenticationStateProvider,
    ExternalAuthenticationClientOptions options,
    ILogger<ExternalAuthenticationController> logger) : Controller
{
    private const string SignInPurpose = "sign-in";
    private const string LocalSignInPurpose = "local-sign-in";
    private static readonly EventId CallbackTransactionUnavailable = new(1001, nameof(CallbackTransactionUnavailable));
    private static readonly EventId CallbackPurposeRejected = new(1002, nameof(CallbackPurposeRejected));
    private static readonly EventId CallbackBrokerFailure = new(1003, nameof(CallbackBrokerFailure));
    private static readonly EventId CallbackStateRejected = new(1004, nameof(CallbackStateRejected));
    private static readonly EventId CallbackCodeMissing = new(1005, nameof(CallbackCodeMissing));
    private static readonly EventId CallbackCompletionFailed = new(1006, nameof(CallbackCompletionFailed));
    private static readonly EventId LocalAuthorizationRedirectRejected = new(1101, nameof(LocalAuthorizationRedirectRejected));
    private static readonly EventId LocalAuthorizationFailed = new(1102, nameof(LocalAuthorizationFailed));
    private static readonly EventId UpstreamLogoutFailed = new(1201, nameof(UpstreamLogoutFailed));
    private static readonly EventId LogoutCallbackRejected = new(1202, nameof(LogoutCallbackRejected));

    [HttpGet("login/{connectionKey}")]
    [AllowAnonymous]
    public IActionResult Login(string connectionKey, [FromQuery] string? returnPath)
    {
        if (string.IsNullOrWhiteSpace(connectionKey))
            return Redirect(ChooserUrl(returnPath));

        var callbackUri = AbsolutePath(options.CallbackPath);
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        transactionStore.Store(Response, new(state, verifier, LocalReturnPath.Normalize(returnPath), DateTimeOffset.UtcNow.AddMinutes(10)));

        var authorizationUri = BackendUriResolver.Resolve(
            anonymousBackendApiClientProvider.Url,
            $"external-authentication/authorize/{Uri.EscapeDataString(connectionKey)}");
        var redirect = QueryHelpers.AddQueryString(authorizationUri.ToString(), new Dictionary<string, string?>
        {
            ["client_id"] = options.ClientId,
            ["redirect_uri"] = callbackUri,
            ["response_type"] = "code",
            ["code_challenge"] = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))),
            ["code_challenge_method"] = "S256",
            ["return_path"] = LocalReturnPath.Normalize(returnPath),
            ["state"] = state
        });
        return Redirect(redirect);
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        if (!transactionStore.TryTake(Request, Response, out var transaction))
        {
            logger.LogWarning(
                CallbackTransactionUnavailable,
                "External authentication callback rejected because the one-time transaction was missing, expired, or invalid. TraceIdentifier: {TraceIdentifier}; BrokerCorrelationId: {BrokerCorrelationId}",
                HttpContext.TraceIdentifier,
                BrokerCorrelationId());
            return Redirect(ChooserUrl("/"));
        }

        if (transaction.Purpose is not (SignInPurpose or LocalSignInPurpose))
        {
            logger.LogWarning(
                CallbackPurposeRejected,
                "External authentication callback rejected because the transaction purpose was invalid. TraceIdentifier: {TraceIdentifier}; BrokerCorrelationId: {BrokerCorrelationId}",
                HttpContext.TraceIdentifier,
                BrokerCorrelationId());
            return Redirect(ChooserUrl("/"));
        }

        var failureCode = transaction.Purpose == LocalSignInPurpose
            ? LoginFailureCodes.SignInFailed
            : LoginFailureCodes.ExternalSignInFailed;

        if (!string.IsNullOrWhiteSpace(error))
        {
            if (!string.IsNullOrWhiteSpace(state) && !StateMatches(state, transaction.State))
            {
                LogCallbackStateRejected();
                return Redirect(ChooserUrl("/"));
            }

            logger.LogWarning(
                CallbackBrokerFailure,
                "External authentication broker reported a sign-in failure. FailureCode: {FailureCode}; TraceIdentifier: {TraceIdentifier}; BrokerCorrelationId: {BrokerCorrelationId}",
                failureCode,
                HttpContext.TraceIdentifier,
                BrokerCorrelationId());
            return Redirect(ChooserUrl(transaction.ReturnPath, failureCode));
        }

        if (string.IsNullOrWhiteSpace(state) || !StateMatches(state, transaction.State))
        {
            LogCallbackStateRejected();
            return Redirect(ChooserUrl("/"));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            logger.LogWarning(
                CallbackCodeMissing,
                "External authentication callback rejected because the completion code was missing. TraceIdentifier: {TraceIdentifier}; BrokerCorrelationId: {BrokerCorrelationId}",
                HttpContext.TraceIdentifier,
                BrokerCorrelationId());
            return Redirect(ChooserUrl(transaction.ReturnPath, failureCode));
        }

        try
        {
            var api = await anonymousBackendApiClientProvider.GetApiAsync<IExternalAuthenticationBrokerApi>(cancellationToken);
            var tokens = await api.ExchangeAsync(
                new("authorization_code", options.ClientId, AbsolutePath(options.CallbackPath), code, transaction.CodeVerifier),
                BasicAuthorization(),
                cancellationToken);
            var principal = ServerExternalAuthenticationStateProvider.CreatePrincipal(tokens.AccessToken);
            await authenticationStateProvider.SignInAsync(HttpContext, principal, tokens, cancellationToken);
            return LocalRedirect(LocalReturnPath.Normalize(transaction.ReturnPath));
        }
        catch (Exception exception)
        {
            logger.LogError(
                CallbackCompletionFailed,
                exception,
                "External authentication callback failed while completing the broker exchange or creating the Studio session. TraceIdentifier: {TraceIdentifier}; BrokerCorrelationId: {BrokerCorrelationId}",
                HttpContext.TraceIdentifier,
                BrokerCorrelationId());
            return Redirect(ChooserUrl(transaction.ReturnPath, failureCode));
        }
    }

    /// <summary>Starts the broker-local credential flow without putting credentials or a verifier in browser storage.</summary>
    [HttpPost("local-login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LocalLogin([FromForm] string? username, [FromForm] string? password, [FromForm] string? returnPath, CancellationToken cancellationToken)
    {
        var callbackUri = AbsolutePath(options.CallbackPath);
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var transaction = new ServerExternalAuthenticationTransaction(
            state,
            verifier,
            LocalReturnPath.Normalize(returnPath),
            DateTimeOffset.UtcNow.AddMinutes(10),
            LocalSignInPurpose);
        transactionStore.Store(Response, transaction);

        try
        {
            var api = await anonymousBackendApiClientProvider.GetApiAsync<IExternalAuthenticationBrokerApi>(cancellationToken);
            var response = await api.AuthorizeLocalAsync(new(
                options.ClientId,
                callbackUri,
                "code",
                WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))),
                "S256",
                transaction.ReturnPath,
                username ?? string.Empty,
                password ?? string.Empty,
                state), cancellationToken);
            if (!Uri.TryCreate(response.RedirectUri, UriKind.Absolute, out var redirectUri) || !string.Equals(redirectUri.GetLeftPart(UriPartial.Path), callbackUri, StringComparison.Ordinal))
            {
                logger.LogWarning(
                    LocalAuthorizationRedirectRejected,
                    "External authentication local sign-in rejected an invalid broker redirect. TraceIdentifier: {TraceIdentifier}",
                    HttpContext.TraceIdentifier);
                return Redirect(ChooserUrl(transaction.ReturnPath, LoginFailureCodes.SignInFailed));
            }
            return Redirect(redirectUri.PathAndQuery);
        }
        catch (Exception exception)
        {
            logger.LogError(
                LocalAuthorizationFailed,
                exception,
                "External authentication local sign-in failed while contacting the broker. TraceIdentifier: {TraceIdentifier}",
                HttpContext.TraceIdentifier);
            return Redirect(ChooserUrl(transaction.ReturnPath, LoginFailureCodes.SignInFailed));
        }
    }

    /// <summary>Completes locally or upstream broker logout, then removes the secure Server cookie.</summary>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = ServerExternalAuthenticationStateProvider.Scheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout([FromForm] string? mode, [FromForm] string? returnPath, CancellationToken cancellationToken)
    {
        var safeReturnPath = LocalReturnPath.Normalize(returnPath);
        try
        {
            var accessToken = await authenticationStateProvider.GetAccessTokenAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                var api = await anonymousBackendApiClientProvider.GetApiAsync<IExternalAuthenticationBrokerApi>(cancellationToken);
                var result = await api.LogoutAsync(new(options.ClientId, AbsolutePath(options.LogoutCallbackPath), mode is "upstream" ? "upstream" : "local"), $"Bearer {accessToken}", cancellationToken);
                await HttpContext.SignOutAsync(ServerExternalAuthenticationStateProvider.Scheme);
                if (!result.Completed && TryGetBackendNavigation(result.NavigationUrl, out var navigationUri))
                {
                    transactionStore.Store(Response, new(
                        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)),
                        string.Empty,
                        safeReturnPath,
                        DateTimeOffset.UtcNow.AddMinutes(10),
                        "logout"));
                    return Redirect(navigationUri.ToString());
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                UpstreamLogoutFailed,
                exception,
                "External authentication upstream logout failed; Studio will continue with local sign-out. TraceIdentifier: {TraceIdentifier}",
                HttpContext.TraceIdentifier);
            // Local sign-out is still safe and must not depend on upstream availability.
        }

        await HttpContext.SignOutAsync(ServerExternalAuthenticationStateProvider.Scheme);
        return LocalRedirect(safeReturnPath);
    }

    [HttpGet("logout-callback")]
    [AllowAnonymous]
    public IActionResult LogoutCallback()
    {
        if (!transactionStore.TryTake(Request, Response, out var transaction) || !string.Equals(transaction.Purpose, "logout", StringComparison.Ordinal))
        {
            logger.LogWarning(
                LogoutCallbackRejected,
                "External authentication logout callback rejected because the one-time transaction was missing, expired, or invalid. TraceIdentifier: {TraceIdentifier}",
                HttpContext.TraceIdentifier);
            return LocalRedirect("/");
        }
        return LocalRedirect(LocalReturnPath.Normalize(transaction.ReturnPath));
    }

    private string AbsolutePath(string path) => $"{Request.Scheme}://{Request.Host}{path}";
    private string ChooserUrl(string? returnPath, string? error = null)
    {
        var query = new Dictionary<string, string?>
        {
            ["choose"] = "true",
            ["returnPath"] = LocalReturnPath.Normalize(returnPath)
        };

        if (!string.IsNullOrWhiteSpace(error))
            query["error"] = error;

        return QueryHelpers.AddQueryString("/login", query);
    }

    private string? BasicAuthorization()
    {
        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            return null;
        return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}"));
    }

    private bool TryGetBackendNavigation(string? navigationUrl, out Uri navigationUri)
    {
        navigationUri = default!;
        if (string.IsNullOrWhiteSpace(navigationUrl))
            return false;
        if (!BackendUriResolver.TryResolveSameOrigin(
                anonymousBackendApiClientProvider.Url,
                navigationUrl,
                out var candidate))
            return false;
        navigationUri = candidate;
        return true;
    }

    private void LogCallbackStateRejected() => logger.LogWarning(
        CallbackStateRejected,
        "External authentication callback rejected because the state was missing or did not match. TraceIdentifier: {TraceIdentifier}; BrokerCorrelationId: {BrokerCorrelationId}",
        HttpContext.TraceIdentifier,
        BrokerCorrelationId());

    private string BrokerCorrelationId()
    {
        var value = Request.Query["correlation_id"].ToString();
        if (value.Length is 0 or > 128)
            return "not-provided";

        return value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            ? value
            : "invalid";
    }

    private static bool StateMatches(string supplied, string expected)
    {
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return suppliedBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }
}
