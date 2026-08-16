using Elsa.Abstractions;
using Elsa.ExternalAuthentication.Constants;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Elsa.Identity.Constants;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Elsa.ExternalAuthentication.Endpoints.Broker;

internal sealed class Logout(IExternalAuthenticationBroker broker) : ElsaEndpoint<LogoutRequest>
{
    public override void Configure()
    {
        Post("/external-authentication/logout");
    }

    public override async Task HandleAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        var sessionId = HttpContext.User.FindFirst(CustomClaimTypes.ExternalAuthenticationSessionId)?.Value;
        if (string.IsNullOrWhiteSpace(sessionId) || !Uri.TryCreate(request.PostLogoutRedirectUri, UriKind.Absolute, out var redirectUri))
        {
            await BrokerEndpointSupport.SendErrorAsync(Send, BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest), cancellationToken);
            return;
        }

        var result = await broker.LogoutAsync(new BrokerLogoutRequest(request.ClientId ?? string.Empty, redirectUri, request.Mode ?? "local"), sessionId, cancellationToken);
        if (result.Error is { } error)
        {
            await BrokerEndpointSupport.SendErrorAsync(Send, error, cancellationToken);
            return;
        }

        await Send.OkAsync(new LogoutResponse(result.Completed, result.NavigationUri?.ToString(), result.RedirectUri?.ToString()), cancellationToken);
    }
}

internal sealed class LogoutRequest
{
    public string? ClientId { get; set; }
    public string? PostLogoutRedirectUri { get; set; }
    public string? Mode { get; set; }
}

internal sealed record LogoutResponse(bool Completed, string? NavigationUrl, string? RedirectUri);

internal sealed class ContinueLogout(IExternalAuthenticationBroker broker) : ElsaEndpointWithoutRequest
{
    public override void Configure() => Get("/external-authentication/logout/continue/{handle}");

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var result = await broker.ContinueLogoutAsync(Route<string>("handle")!, cancellationToken);
        if (result.Error is not null || result.NavigationUri is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        HttpContext.Response.Redirect(result.NavigationUri.ToString());
    }
}
