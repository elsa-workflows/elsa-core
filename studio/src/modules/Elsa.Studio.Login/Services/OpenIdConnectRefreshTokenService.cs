using Elsa.Api.Client.Resources.Identity.Responses;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Elsa.Studio.Login.Services;

///<inheritdoc/>
public class OpenIdConnectRefreshTokenService(IOptions<OpenIdConnectConfiguration> options, IJwtAccessor jwtAccessor, HttpClient httpClient) : IRefreshTokenService
{
    ///<inheritdoc/>
    public async Task<LoginResponse> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        // Get refresh token.
        var refreshToken = await jwtAccessor.ReadTokenAsync(TokenNames.RefreshToken);
        if (refreshToken == null)
            return new(false, null, null);

        // Setup request to get new tokens.
        var refreshRequestMessage = new HttpRequestMessage(HttpMethod.Post, options.Value.TokenEndpoint);
        refreshRequestMessage.Content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("client_id", options.Value.ClientId),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
        ]);

        // Send request.
        var response = await httpClient.SendAsync(refreshRequestMessage, cancellationToken);

        // If the refresh token is invalid, we can't do anything.
        if (!response.IsSuccessStatusCode)
            return new(false, null, null);

        // Parse response into tokens.
        var tokens = (await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken))!;

        // Store tokens.
        await jwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, tokens.RefreshToken ?? "");
        await jwtAccessor.WriteTokenAsync(TokenNames.AccessToken, tokens.AccessToken ?? "");
        await jwtAccessor.WriteTokenAsync(TokenNames.IdToken, tokens.IdToken ?? "");

        // Return tokens.
        return new(tokens.AccessToken != null, tokens.AccessToken, tokens.RefreshToken);
    }
}
