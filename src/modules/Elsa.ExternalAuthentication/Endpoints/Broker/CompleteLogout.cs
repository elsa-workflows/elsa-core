using Elsa.Abstractions;
using Elsa.ExternalAuthentication.Constants;
using Elsa.ExternalAuthentication.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;

namespace Elsa.ExternalAuthentication.Endpoints.Broker;

internal sealed class CompleteLogout(IExternalAuthenticationBroker broker) : ElsaEndpoint<CompleteLogoutRequest>
{
    public override void Configure()
    {
        Get("/external-authentication/logout/callback/{connectionId}");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(ExternalAuthenticationRateLimitPolicyNames.ProviderCallback));
    }

    public override async Task HandleAsync(CompleteLogoutRequest request, CancellationToken cancellationToken)
    {
        var result = await broker.CompleteLogoutAsync(Route<string>("connectionId") ?? request.ConnectionId ?? string.Empty, Query<string>("state", false) ?? request.State ?? string.Empty, cancellationToken);
        if (result.Error is { } error)
        {
            await BrokerEndpointSupport.SendErrorAsync(Send, error, cancellationToken);
            return;
        }

        await Send.RedirectAsync(result.RedirectUri!.ToString(), false, true);
    }
}

internal sealed class CompleteLogoutRequest
{
    public string? ConnectionId { get; set; }
    public string? State { get; set; }
}
