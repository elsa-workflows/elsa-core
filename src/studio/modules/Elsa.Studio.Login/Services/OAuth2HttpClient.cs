using System.Net.Http.Json;
using Elsa.Studio.Login.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Login.Services;

/// <summary>
/// Provides functionality for interacting with an OAuth2 token endpoint to request tokens.
/// </summary>
public class OAuth2HttpClient(HttpClient httpClient, IOptions<OAuth2CredentialsValidatorOptions> options)
{
    private readonly OAuth2CredentialsValidatorOptions _options = options.Value;

    /// <summary>
    /// Sends a token request to the OAuth2 token endpoint using the specified credentials.
    /// </summary>
    /// <param name="username">The username to authenticate with.</param>
    /// <param name="password">The password to authenticate with.</param>
    /// <param name="cancellationToken">A cancellation token used to cancel the operation.</param>
    /// <returns>A <see cref="TokenResponse"/> containing the access token, refresh token, and other token details.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the response cannot be read or contains no valid data.</exception>
    public async Task<TokenResponse> RequestTokenAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint);
        var content = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "username", username },
            { "password", password },
            { "client_id", _options.ClientId },
        };

        if (!string.IsNullOrEmpty(_options.ClientSecret)) content["client_secret"] = _options.ClientSecret;
        if (!string.IsNullOrEmpty(_options.Scope)) content["scope"] = _options.Scope;

        request.Content = new FormUrlEncodedContent(content);

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new();

        return await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Failed to read token response.");
    }
}