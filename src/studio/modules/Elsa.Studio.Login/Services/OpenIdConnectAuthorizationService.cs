using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;

namespace Elsa.Studio.Login.Services;

/// <inheritdoc/>
public class OpenIdConnectAuthorizationService(IJwtAccessor jwtAccessor, IOptions<OpenIdConnectConfiguration> configuration, NavigationManager navigationManager, HttpClient httpClient, IOpenIdConnectPkceStateService pkceStateService, ILogger<OpenIdConnectAuthorizationService> logger) : IAuthorizationService
{
    private const int MaxLoggedErrorLength = 1024;
    private static readonly string[] CorrelationIdHeaderNames = ["traceparent", "request-id", "x-request-id", "x-correlation-id", "x-ms-request-id", "x-ms-correlation-id"];

    /// <inheritdoc/>
    public async Task RedirectToAuthorizationServer()
    {
        var config = configuration.Value;
        var redirectUri = new Uri(navigationManager.Uri).GetLeftPart(UriPartial.Authority) + "/signin-oidc";
        string url = config.AuthEndpoint + $"?client_id={WebUtility.UrlEncode(config.ClientId)}&redirect_uri={WebUtility.UrlEncode(redirectUri)}&response_type=code&scope={WebUtility.UrlEncode(String.Join(' ', config.Scopes))}";
        if (config.UsePkce)
        {
            var generated = await pkceStateService.GeneratePkceCodeChallenge();
            url += $"&code_challenge={generated.CodeChallenge}&code_challenge_method={generated.Method}";
        }
        if (navigationManager.ToBaseRelativePath(navigationManager.Uri) is { } returnUrl and not "/")
        {
            url += "&state=" + WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(returnUrl));
        }

        navigationManager.NavigateTo(url, true);
    }

    /// <inheritdoc/>
    public async Task ReceiveAuthorizationCode(string code, string? state, CancellationToken cancellationToken)
    {
        var config = configuration.Value;
        var redirectUri = new Uri(navigationManager.Uri).GetLeftPart(UriPartial.Authority) + "/signin-oidc";

        var formValues = new List<KeyValuePair<string, string>>
        {
            new("client_id", config.ClientId),
            new("code", code),
            new("grant_type", "authorization_code"),
            new("redirect_uri", redirectUri)
        };

        if(!string.IsNullOrWhiteSpace(config.ClientSecret))
        {
            formValues.Add(new KeyValuePair<string, string>("client_secret", config.ClientSecret));
        }

        if (config.UsePkce)
        {
            var codeVerifier = await pkceStateService.GetPkceCodeVerifier();
            formValues.Add(new KeyValuePair<string, string>("code_verifier", codeVerifier));
        }

        var refreshRequestMessage = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(formValues)
        };

        // Send request.
        using var response = await httpClient.SendAsync(refreshRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await ThrowTokenExchangeExceptionAsync(response, config, cancellationToken);

        var tokens = (await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken))!;

        await jwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, tokens.RefreshToken ?? "");
        await jwtAccessor.WriteTokenAsync(TokenNames.AccessToken, tokens.AccessToken ?? "");
        await jwtAccessor.WriteTokenAsync(TokenNames.IdToken, tokens.IdToken ?? "");

        string returnUrl = "/";
        if (!String.IsNullOrWhiteSpace(state))
        {
            returnUrl = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(state));
        }
        navigationManager.NavigateTo(returnUrl, true);
    }

    private async Task ThrowTokenExchangeExceptionAsync(HttpResponseMessage response, OpenIdConnectConfiguration config, CancellationToken cancellationToken)
    {
        var errorSummary = await ReadErrorSummaryAsync(response.Content, cancellationToken);
        var correlationIds = GetCorrelationIds(response);

        logger.LogWarning(
            "OIDC token exchange failed for endpoint {TokenEndpoint}. Status code: {StatusCode}. Reason phrase: {ReasonPhrase}. Correlation IDs: {CorrelationIds}. Error summary: {ErrorSummary}",
            config.TokenEndpoint,
            response.StatusCode,
            response.ReasonPhrase,
            string.IsNullOrWhiteSpace(correlationIds) ? "none" : correlationIds,
            errorSummary);

        var message = $"""
            Authentication failed while completing the OpenID Connect sign-in flow.
            Status Code: {response.StatusCode}
            """;

        if (!string.IsNullOrWhiteSpace(correlationIds))
            message += $"\nCorrelation IDs: {correlationIds}";

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private static string GetCorrelationIds(HttpResponseMessage response)
    {
        var headers = CorrelationIdHeaderNames
            .SelectMany(headerName => response.Headers.TryGetValues(headerName, out var values) ? values : [])
            .Distinct()
            .ToArray();

        return headers.Length > 0 ? string.Join(", ", headers) : string.Empty;
    }

    private static async Task<string> ReadErrorSummaryAsync(HttpContent content, CancellationToken cancellationToken)
    {
        var (body, truncated) = await ReadContentSnippetAsync(content, cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
            return truncated ? "Response body omitted because it exceeded the configured limit." : "Response body was empty.";

        var summary = TryParseErrorSummary(body) ?? body;
        summary = string.Join(" ", summary.Split((string[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (summary.Length > MaxLoggedErrorLength)
            summary = summary[..MaxLoggedErrorLength];

        return truncated ? $"{summary}…" : summary;
    }

    private static async Task<(string Content, bool Truncated)> ReadContentSnippetAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var buffer = new char[MaxLoggedErrorLength + 1];
        var totalRead = 0;

        while (totalRead < buffer.Length)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken);
            if (read == 0)
                break;

            totalRead += read;
        }

        return (new string(buffer, 0, Math.Min(totalRead, MaxLoggedErrorLength)), totalRead > MaxLoggedErrorLength);
    }

    private static string? TryParseErrorSummary(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (root.TryGetProperty("error_description", out var errorDescription) && errorDescription.GetString() is { Length: > 0 } description)
                return description;

            if (root.TryGetProperty("error", out var error) && error.GetString() is { Length: > 0 } errorCode)
                return errorCode;

            if (root.TryGetProperty("message", out var message) && message.GetString() is { Length: > 0 } errorMessage)
                return errorMessage;
        }
        catch (JsonException)
        {
            // Best-effort JSON parsing only; if parsing fails, the caller falls back to the raw response snippet.
        }

        return null;
    }
}
