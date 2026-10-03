using System.Net.Http.Json;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
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
/// </summary>
public class ElsaIdentitySignOutService(
    IJwtAccessor jwtAccessor,
    IJwtParser jwtParser,
    ITokenProvider tokenProvider,
    IRemoteBackendAccessor remoteBackendAccessor,
    IHttpClientFactory httpClientFactory,
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

        await jwtAccessor.ClearTokenAsync(TokenNames.AccessToken);
        await jwtAccessor.ClearTokenAsync(TokenNames.RefreshToken);

        if (authenticationStateProvider is AccessTokenAuthenticationStateProvider accessTokenAuthenticationStateProvider)
        {
            accessTokenAuthenticationStateProvider.NotifyAuthenticationStateChanged();
        }

        navigationManager.NavigateTo(LoginPath, forceLoad: true);
    }

    private async Task RevokeSessionAsync()
    {
        try
        {
            using var timeout = new CancellationTokenSource(RevokeTimeout, timeProvider);
            await RevokeSessionAsync(timeout.Token).WaitAsync(timeout.Token);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Revoking the session on sign-out failed; signing out locally.");
        }
    }

    private async Task RevokeSessionAsync(CancellationToken cancellationToken)
    {
        var accessToken = await jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);
        var refreshToken = await jwtAccessor.ReadTokenAsync(TokenNames.RefreshToken);

        if (string.IsNullOrWhiteSpace(accessToken) || IsExpired(accessToken))
        {
            accessToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
            refreshToken = await jwtAccessor.ReadTokenAsync(TokenNames.RefreshToken);
        }

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, remoteBackendAccessor.RemoteBackend.Url + "/identity/logout");
        request.Headers.Authorization = new("Bearer", accessToken);
        request.Content = JsonContent.Create(new LogoutRequest(refreshToken), ElsaIdentityLogoutJsonContext.Default.LogoutRequest);

        using var response = await httpClientFactory.CreateClient(ElsaIdentityRefreshTokenService.AnonymousClientName).SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("The backend did not revoke the session on sign-out (status {StatusCode}); signing out locally.", response.StatusCode);
        }
    }

    private bool IsExpired(string accessToken)
    {
        try
        {
            return jwtParser.Parse(accessToken).IsExpired();
        }
        catch
        {
            return false;
        }
    }
}
