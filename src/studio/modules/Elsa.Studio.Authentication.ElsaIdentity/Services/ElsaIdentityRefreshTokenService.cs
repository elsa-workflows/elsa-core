using System.Net;
using System.Net.Http.Json;
using Elsa.Api.Client.Resources.Identity.Responses;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Authentication.ElsaIdentity.Services;

///<inheritdoc/>
public class ElsaIdentityRefreshTokenService(IRemoteBackendAccessor remoteBackendAccessor, IJwtAccessor jwtAccessor, IHttpClientFactory httpClientFactory) : IRefreshTokenService
{
    internal const string AnonymousClientName = "Elsa.Studio.Authentication.ElsaIdentity.Anonymous";

    ///<inheritdoc/>
    public async Task<LoginResponse> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        // Get refresh token.
        var refreshToken = await jwtAccessor.ReadTokenAsync(TokenNames.RefreshToken);

        if (string.IsNullOrWhiteSpace(refreshToken))
            return new(false, null, null);

        // Setup request to get new tokens.
        var url = remoteBackendAccessor.RemoteBackend.Url + "/identity/refresh-token";
        using var refreshRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        refreshRequestMessage.Headers.Authorization = new("Bearer", refreshToken);

        // IMPORTANT: Use an anonymous HttpClient (no AuthenticatingApiHttpMessageHandler) to avoid recursion.
        var httpClient = httpClientFactory.CreateClient(AnonymousClientName);

        // Send request.
        var response = await httpClient.SendAsync(refreshRequestMessage, cancellationToken);

        // If the refresh token is invalid, we can't do anything.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return new(false, null, null);

        // Parse response into tokens.
        var tokens = (await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken))!;

        // Store tokens.
        if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
            await jwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, tokens.RefreshToken);

        if (!string.IsNullOrWhiteSpace(tokens.AccessToken))
            await jwtAccessor.WriteTokenAsync(TokenNames.AccessToken, tokens.AccessToken);

        // Return tokens.
        return tokens;
    }
}
