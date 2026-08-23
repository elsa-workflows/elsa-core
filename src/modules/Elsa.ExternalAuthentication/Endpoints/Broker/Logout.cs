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
        // Deliberately authenticated without a permission: the session id is read from the caller's
        // principal below, so an identity is required, but logging out is never permission-gated.
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
    public override void Configure()
    {
        Get("/external-authentication/logout/continue/{handle}");

        // Anonymous, like every other broker endpoint the browser is navigated to. The single-use
        // route handle carries the authority; the caller's Elsa session has already been revoked by
        // the time this runs, and a top-level browser navigation sends no Authorization header, so
        // this endpoint can never present authenticated credentials.
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var result = await broker.ContinueLogoutAsync(Route<string>("handle")!, cancellationToken);
        if (result.Error is { } error)
        {
            await BrokerEndpointSupport.SendErrorAsync(Send, error, cancellationToken);
            return;
        }

        if (result.NavigationUri is null)
        {
            await BrokerEndpointSupport.SendErrorAsync(Send, BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest), cancellationToken);
            return;
        }

        // Responses must go through the Send API: writing to HttpContext.Response without starting it
        // lets the FastEndpoints auto-response overwrite the status with 204, which silently discarded
        // both the redirect and the error this endpoint used to produce.
        await Send.RedirectAsync(result.NavigationUri.ToString(), false, true);
    }
}
