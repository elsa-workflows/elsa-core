using Elsa.Abstractions;
using Elsa.ExternalAuthentication.Constants;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;

namespace Elsa.ExternalAuthentication.Endpoints.Broker;

internal sealed class CompleteCallback(IExternalAuthenticationBroker broker) : ElsaEndpoint<CompleteCallbackRequest>
{
    public override void Configure()
    {
        Get("/external-authentication/callback/{connectionId}");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(ExternalAuthenticationRateLimitPolicyNames.ProviderCallback));
    }

    public override async Task HandleAsync(CompleteCallbackRequest request, CancellationToken cancellationToken)
    {
        var parameters = HttpContext.Request.Query.ToDictionary(x => x.Key, x => (IReadOnlyCollection<string>)x.Value.Where(value => value is not null).Select(value => value!).ToArray(), StringComparer.Ordinal);
        var result = await broker.CompleteCallbackAsync(Route<string>("connectionId") ?? request.ConnectionId ?? string.Empty, Query<string>("state", false) ?? request.State ?? string.Empty, parameters, cancellationToken);
        if (result.Error is { } error)
        {
            if (result.RedirectUri is not null)
            {
                await Send.RedirectAsync(result.RedirectUri.ToString(), false, true);
                return;
            }
            await BrokerEndpointSupport.SendErrorAsync(Send, error, cancellationToken);
            return;
        }

        await Send.RedirectAsync(result.RedirectUri!.ToString(), false, true);
    }
}

internal sealed class CompleteCallbackRequest
{
    public string? ConnectionId { get; set; }
    public string? State { get; set; }
}
