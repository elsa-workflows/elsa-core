using System.Net.Http.Json;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Extensions;
using Elsa.Studio.Authentication.ElsaIdentity.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.Authentication.ElsaIdentity.Services;

/// <summary>
/// Signs out by revoking the sign-in session at the backend, then discarding the stored access and refresh tokens.
/// Revoking is best effort: the local session ends even when the backend cannot be reached or refuses.
/// The service is created by dependency injection; its constructor dependencies are all resolved from the container.
/// </summary>
public class ElsaIdentitySignOutService(
    IJwtAccessor jwtAccessor,
    IJwtParser jwtParser,
    ITokenProvider tokenProvider,
    IRemoteBackendAccessor remoteBackendAccessor,
    IHttpClientFactory httpClientFactory,
    ElsaIdentitySessionGate sessionGate,
    AuthenticationStateProvider authenticationStateProvider,
    NavigationManager navigationManager,
    TimeProvider timeProvider,
    ILogger<ElsaIdentitySignOutService> logger) : ISignOutService
{
    /// <summary>
    /// The page the user lands on after signing out.
    /// </summary>
    public const string LoginPath = "/login";

    private static readonly TimeSpan RevokeTimeout = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public async Task SignOutAsync()
    {
        await RevokeSessionAsync();

        // Serialized with a refresh storing its response, so the refresh sees the cleared session and backs off.
        await sessionGate.RunAsync(async () =>
        {
            await jwtAccessor.ClearTokensAsync();
            return true;
        });

        if (authenticationStateProvider is AccessTokenAuthenticationStateProvider accessTokenAuthenticationStateProvider)
            accessTokenAuthenticationStateProvider.NotifyAuthenticationStateChanged();

        // Force a reload so no in-memory state from the signed-out user survives.
        navigationManager.NavigateTo(LoginPath, forceLoad: true);
    }

    private async Task RevokeSessionAsync()
    {
        try
        {
            // Neither an unreachable backend nor a token store that does not answer may keep the user signed in, so
            // the whole attempt is bounded, not only the calls that observe the cancellation token.
            using var timeout = new CancellationTokenSource(RevokeTimeout, timeProvider);
            await RevokeSessionAsync(timeout.Token).WaitAsync(timeout.Token);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Revoking the session on sign-out failed; signing out locally.");
        }
    }

    // Neither the refresh nor the request runs inside the session gate, because a refresh takes the gate to store its response.
    private async Task RevokeSessionAsync(CancellationToken cancellationToken)
    {
        var accessToken = await jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);
        var refreshToken = await jwtAccessor.ReadTokenAsync(TokenNames.RefreshToken);

        // The endpoint requires a valid access token. One that has not expired is sent as it is: the token provider
        // would refresh it early, and a rejected refresh clears the tokens before the session is revoked.
        if (string.IsNullOrWhiteSpace(accessToken) || IsExpired(accessToken))
        {
            accessToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
            refreshToken = await jwtAccessor.ReadTokenAsync(TokenNames.RefreshToken);
        }

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            return;

        using var request = new HttpRequestMessage(HttpMethod.Post, remoteBackendAccessor.RemoteBackend.Url + "/identity/logout");
        request.Headers.Authorization = new("Bearer", accessToken);
        request.Content = JsonContent.Create(new LogoutRequest(refreshToken), ElsaIdentityLogoutJsonContext.Default.LogoutRequest);

        // IMPORTANT: Use an anonymous HttpClient (no AuthenticatingApiHttpMessageHandler) to avoid recursion.
        using var response = await httpClientFactory.CreateClient(ElsaIdentityRefreshTokenService.AnonymousClientName).SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            logger.LogWarning("The backend did not revoke the session on sign-out (status {StatusCode}); signing out locally.", response.StatusCode);
    }

    private bool IsExpired(string accessToken)
    {
        try
        {
            return jwtParser.Parse(accessToken).IsExpired();
        }
        catch
        {
            // A token that cannot be read is left for the backend to judge.
            return false;
        }
    }
}
