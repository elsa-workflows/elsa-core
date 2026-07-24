using Elsa.Abstractions;
using Elsa.ExternalAuthentication.Constants;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;

namespace Elsa.ExternalAuthentication.Endpoints.Broker;

internal sealed class ExchangeToken(IExternalAuthenticationBroker broker) : ElsaEndpoint<ExchangeTokenRequest>
{
    public override void Configure()
    {
        Post("/external-authentication/token");
        AllowAnonymous();
        AllowFormData(true);
        Options(x => x.RequireRateLimiting(ExternalAuthenticationRateLimitPolicyNames.TokenExchange));
    }

    public override async Task HandleAsync(ExchangeTokenRequest request, CancellationToken cancellationToken)
    {
        Uri? redirectUri = null;
        if (!string.IsNullOrWhiteSpace(request.RedirectUri) && !Uri.TryCreate(request.RedirectUri, UriKind.Absolute, out redirectUri))
        {
            await BrokerEndpointSupport.SendErrorAsync(Send, BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest), cancellationToken);
            return;
        }

        var basicCredentials = TryGetBasicCredentials(HttpContext.Request.Headers.Authorization);
        var result = await broker.ExchangeAsync(new BrokerTokenRequest(request.GrantType ?? string.Empty, request.ClientId ?? string.Empty, redirectUri, request.Code, request.CodeVerifier, request.RefreshToken, HttpContext.Request.Headers.Origin, basicCredentials?.ClientId, basicCredentials?.Secret), cancellationToken);
        if (result.Error is { } error)
        {
            await BrokerEndpointSupport.SendErrorAsync(Send, error, cancellationToken);
            return;
        }

        await Send.OkAsync(result.Token!, cancellationToken);
    }

    private static (string ClientId, string Secret)? TryGetBasicCredentials(string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return null;
        try
        {
            var parts = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(authorization[6..])).Split(':', 2);
            return parts.Length == 2 ? (parts[0], parts[1]) : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

internal sealed class ExchangeTokenRequest
{
    public string? GrantType { get; set; }
    public string? ClientId { get; set; }
    public string? RedirectUri { get; set; }
    public string? Code { get; set; }
    public string? CodeVerifier { get; set; }
    public string? RefreshToken { get; set; }
}
